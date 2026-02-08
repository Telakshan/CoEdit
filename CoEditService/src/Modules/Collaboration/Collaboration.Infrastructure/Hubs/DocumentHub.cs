using System.Diagnostics.CodeAnalysis;
using Collaboration.Application.Services;
using Collaboration.Core.Abstract;
using Collaboration.Core.Entities;
using Collaboration.Core.Operations;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using CoEdit.Common.Application.Dto;
using CoEdit.Common.Application.IntegrationEvents;

namespace CoEdit.Modules.Collaboration.Hubs;

[ExcludeFromCodeCoverage(Justification = "SignalR hub with group management and broadcasting;")]
public class DocumentHub: Hub
{
    private readonly ISessionStateService _sessionStateService;
    private readonly IOperationTransform _operationTransform;
    private readonly IPublisher _publisher;
    private readonly IDistributedLockService _lockService;

    public DocumentHub(ISessionStateService sessionStateService,
        IOperationTransform operationTransform,
        IPublisher publisher,
        IDistributedLockService lockService)
    {
        _sessionStateService = sessionStateService;
        _operationTransform = operationTransform;
        _publisher = publisher;
        _lockService = lockService;
    }

    public async Task JoinDocument(Guid documentId)
    {
        var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid userId = Guid.Empty;
        
        if (!string.IsNullOrEmpty(userIdString))
        {
            Guid.TryParse(userIdString, out userId);
        }

        string groupName = documentId.ToString();
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        var session = new EditSession(documentId, userId, groupName);
        await _sessionStateService.AddSessionAsync(session);

        await Clients.Group(groupName).SendAsync(
            "UserJoined",
            new UserJoinedDto
            {
                UserId = userId,
                DisplayName = session.DisplayName,
                ConnectionId = session.ConnectionId,
                JoinedAt = session.JoinedAt
            });

        await _publisher.Publish(new UserJoinedDocumentIntegrationEvent(userId, documentId, Context.ConnectionId));
    }

    public async Task LeaveDocument(Guid documentId)
    {
        var groupName = documentId.ToString();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await _sessionStateService.RemoveSessionAsync(documentId, Context.ConnectionId);
        
        var userIdString = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid userId = Guid.Empty;
        
        if (!string.IsNullOrEmpty(userIdString))
        {
            Guid.TryParse(userIdString, out userId);
        }

        await Clients.Group(groupName).SendAsync(
            "UserLeft",
            new UserLeftDto 
                { 
                    ConnectionId = Context.ConnectionId,
                    UserId = userId 
                });
        
        await _publisher.Publish(new UserLeftDocumentIntegrationEvent(userId, documentId, Context.ConnectionId));
    }
    
    public async Task SendOperation(Guid documentId, OperationDto operationDto)
    {
        IAsyncDisposable? lockHandle = null;
        for (int i = 0; i < 5; i++)
        {
            lockHandle = await _lockService.AcquireLockAsync(documentId.ToString(), TimeSpan.FromSeconds(5));
            if (lockHandle != null) break;
            await Task.Delay(50);
        }

        if (lockHandle == null)
            throw new HubException("Could not acquire lock for document operation processing.");

        try
        {
            var currentVersion = await _sessionStateService.GetVersionAsync(documentId);
            var op = operationDto.ToEntity();

            if (op.Version <= currentVersion)
            {
                var missedOps = await _sessionStateService.GetOperationsAfterVersionAsync(documentId, op.Version);
                var transformedOps = _operationTransform.TransformAgainstConcurrent(op, missedOps);
                op = transformedOps.First();
            }

            var newVersion = await _sessionStateService.IncrementVersionAsync(documentId);
            op.Version = (int)newVersion;

            await _sessionStateService.AddOperationAsync(op);

            await Clients.Group(documentId.ToString()).SendAsync("ReceiveOperation", op);
        }
        finally
        {
            if (lockHandle != null)
                await lockHandle.DisposeAsync();
        }
    }
    
    public async Task UpdateCursor(Guid documentId, string displayName, int offset, int? selectionEnd)
    {
        await Clients.OthersInGroup(documentId.ToString()).SendAsync("CursorMoved", new
        {
            ConnectionId = Context.ConnectionId,
            DisplayName = displayName,
            Cursor = new { Position = offset, SelectionEnd = selectionEnd }
        });
    }
    
    public async Task SyncContent(Guid documentId, string htmlContent)
    {
        await Clients.OthersInGroup(documentId.ToString()).SendAsync("ReceiveContent", htmlContent);
    }

    public async Task StartTyping(Guid documentId)
    {
        var userIdString = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await Clients.Group(documentId.ToString()).SendAsync("UserTyping", new { UserId = userIdString, IsTyping = true });
    }

    public async Task StopTyping(Guid documentId)
    {
        var userIdString = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await Clients.Group(documentId.ToString()).SendAsync("UserTyping", new { UserId = userIdString, IsTyping = false });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
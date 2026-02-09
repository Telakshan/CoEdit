using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using CoEdit.Common.Application.Dto;
using CoEdit.Common.Application.IntegrationEvents;
using Collaboration.Application.Services;
using Collaboration.Domain.Abstract;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Operations;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Collaboration.Infrastructure.Hubs;

[ExcludeFromCodeCoverage(Justification = "SignalR hub with group management and broadcasting;")]
public class DocumentHub(
    ISessionStateService sessionStateService,
    IOperationalTransform operationTransform,
    IPublisher publisher,
    IDistributedLockService lockService)
    : Hub
{
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
        await sessionStateService.AddSessionAsync(session);

        await Clients.Group(groupName).SendAsync(
            "UserJoined",
            new UserJoinedDto
            {
                UserId = userId,
                DisplayName = session.DisplayName,
                ConnectionId = session.ConnectionId,
                JoinedAt = session.JoinedAt
            });

        await publisher.Publish(new UserJoinedDocumentIntegrationEvent(userId, documentId, Context.ConnectionId));
    }

    public async Task LeaveDocument(Guid documentId)
    {
        var groupName = documentId.ToString();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await sessionStateService.RemoveSessionAsync(documentId, Context.ConnectionId);
        
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
        
        await publisher.Publish(new UserLeftDocumentIntegrationEvent(userId, documentId, Context.ConnectionId));
    }
    
    public async Task SendOperation(Guid documentId, OperationDto operationDto)
    {
        IAsyncDisposable? lockHandle = null;
        for (int i = 0; i < 5; i++)
        {
            lockHandle = await lockService.AcquireLockAsync(documentId.ToString(), TimeSpan.FromSeconds(5));
            if (lockHandle != null) break;
            await Task.Delay(50);
        }

        if (lockHandle == null)
            throw new HubException("Could not acquire lock for document operation processing.");

        try
        {
            var currentVersion = await sessionStateService.GetVersionAsync(documentId);
            var op = operationDto.ToEntity();

            if (op.Version <= currentVersion)
            {
                var missedOps = await sessionStateService.GetOperationsAfterVersionAsync(documentId, op.Version);
                var transformedOps = operationTransform.TransformAgainstConcurrent(op, missedOps);
                op = transformedOps.First();
            }

            var newVersion = await sessionStateService.IncrementVersionAsync(documentId);
            op.Version = (int)newVersion;

            await sessionStateService.AddOperationAsync(op);

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
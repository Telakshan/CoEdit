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

[Authorize]
public class DocumentHub : Hub
{
    private const string SnapshotSyncDeprecatedError =
        "Snapshot content sync is no longer supported. Clients must use OT SendOperation.";

    private readonly ISessionStateService _sessionService;
    private readonly IDocumentPersistenceQueue _documentPersistenceQueue;
    private readonly ICursorBroadcastScheduler _cursorBroadcastScheduler;
    private readonly IPublisher _publisher;
    private readonly ISender _sender;
    private readonly IDocumentAuthorizationService _authorizationService;

    private Result<Guid> GetCurrentUserId()
    {
        var userIdString = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdString, out var userId) && userId != Guid.Empty)
        {
            return Result.Success(userId);
        }
        return Result.Failure<Guid>("User identifier claim not found or invalid.");
    }

    public DocumentHub(
        ISessionStateService sessionService,
        IDocumentPersistenceQueue documentPersistenceQueue,
        ICursorBroadcastScheduler cursorBroadcastScheduler,
        IPublisher publisher,
        ISender sender,
        IDocumentAuthorizationService authorizationService)
    {
        _sessionService = sessionService;
        _documentPersistenceQueue = documentPersistenceQueue;
        _cursorBroadcastScheduler = cursorBroadcastScheduler;
        _publisher = publisher;
        _sender = sender;
        _authorizationService = authorizationService;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var affectedDocuments = await _sessionService.RemoveSessionFromAllDocumentsAsync(Context.ConnectionId);
        var userIdResult = GetCurrentUserId();

        foreach (var documentId in affectedDocuments)
        {
            var groupName = documentId.ToString();

            if (userIdResult.IsSuccess)
            {
                var userId = userIdResult.Value;
                await Clients.Group(groupName).SendAsync("UserLeft", new { ConnectionId = Context.ConnectionId, UserId = userId });
                await _publisher.Publish(new UserLeftDocumentIntegrationEvent(userId, documentId, Context.ConnectionId));
            }
            else
            {
                await Clients.Group(groupName).SendAsync("UserLeft", new { ConnectionId = Context.ConnectionId });
            }

            await FlushPendingDocumentPersistenceIfIdle(documentId, Context.ConnectionAborted);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinDocument(Guid documentId)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsFailure)
        {
            await Clients.Caller.SendAsync("Error", userIdResult.Error);
            return;
        }

        var userId = userIdResult.Value;

        if (!await _authorizationService.CanAccessAsync(userId, documentId, AccessLevel.Viewer))
        {
            await Clients.Caller.SendAsync("Error", "You do not have permission to access this document.");
            return;
        }

        var groupName = documentId.ToString();
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        var session = new EditSession(documentId, userId, Context.ConnectionId);
        await _sessionService.AddSessionAsync(session);

        var currentContent = await _sessionService.GetDocumentContentAsync(documentId) ?? string.Empty;
        await Clients.Caller.SendAsync("ReceiveContent", ContentSyncEnvelopeCodec.SerializeSnapshot(currentContent));
        await Clients.Caller.SendAsync("ReceiveVersion", (int)await _sessionService.GetVersionAsync(documentId));
        await SendTransportModeToCaller();

        await Clients.Group(groupName).SendAsync("UserJoined", new UserJoinedDto
        {
            UserId = userId,
            // ConnectionId isn't in UserJoinedDto currently, but DisplayName/Color are
            DisplayName = "", // You might want to fetch this
            JoinedAt = DateTime.UtcNow
        });

        await _publisher.Publish(new UserJoinedDocumentIntegrationEvent(userId, documentId, Context.ConnectionId));
    }

    public async Task RequestDocumentState(Guid documentId)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsFailure)
        {
            await Clients.Caller.SendAsync("Error", userIdResult.Error);
            return;
        }

        if (!await _authorizationService.CanAccessAsync(userIdResult.Value, documentId, AccessLevel.Viewer))
        {
            await Clients.Caller.SendAsync("Error", "You do not have permission to access this document.");
            return;
        }

        await SendSnapshotResyncToCaller(documentId);
        await SendTransportModeToCaller();
    }

    public async Task LeaveDocument(Guid documentId)
    {
        var groupName = documentId.ToString();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await _sessionService.RemoveSessionAsync(documentId, Context.ConnectionId);
        await FlushPendingDocumentPersistenceIfIdle(documentId, Context.ConnectionAborted);

        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsFailure)
        {
            await Clients.Caller.SendAsync("Error", userIdResult.Error);
            return;
        }

        var userId = userIdResult.Value;
        await Clients.Group(groupName).SendAsync("UserLeft", new { ConnectionId = Context.ConnectionId, UserId = userId });

        await _publisher.Publish(new UserLeftDocumentIntegrationEvent(userId, documentId, Context.ConnectionId));
    }

    public async Task SendOperation(Guid documentId, OperationDto operationDto)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsFailure)
        {
            await Clients.Caller.SendAsync("Error", userIdResult.Error);
            return;
        }

        if (!await _authorizationService.CanAccessAsync(userIdResult.Value, documentId, AccessLevel.Editor))
        {
            await Clients.Caller.SendAsync("Error", "You do not have permission to edit this document.");
            return;
        }

        var commandDto = new OperationDto
        {
            DocumentId = documentId,
            UserId = userIdResult.Value,
            ClientOperationId = operationDto.ClientOperationId,
            Type = operationDto.Type,
            Position = operationDto.Position,
            Length = operationDto.Length,
            Content = operationDto.Content,
            BaseVersion = operationDto.BaseVersion == 0 && operationDto.Version > 0
                ? operationDto.Version
                : operationDto.BaseVersion
        };

        var result = await _sender.Send(new ProcessOperationCommand(commandDto));

        if (result.IsFailure)
        {
            var rejectionPayload = OperationRejectionPayloadFactory.Create(documentId, commandDto, result.Error);
            await Clients.Caller.SendAsync("ReceiveOperationRejected", rejectionPayload);
            await Clients.Caller.SendAsync("Error", rejectionPayload.Error);
            if (rejectionPayload.RequiresResync)
            {
                await SendSnapshotResyncToCaller(documentId);
            }
            return;
        }

        var updatedContent = await _sessionService.GetDocumentContentAsync(documentId);
        if (updatedContent is not null)
        {
            _documentPersistenceQueue.Enqueue(
                documentId,
                userIdResult.Value,
                updatedContent,
                "DocumentHub.SendOperation");
        }

        await Clients.Caller.SendAsync("ReceiveOperationAck", result.Value);
        await Clients.OthersInGroup(documentId.ToString()).SendAsync("ReceiveOperation", result.Value);
    }

    public async Task UpdateCursor(Guid documentId, string displayName, int offset, int? selectionEnd, double? top, double? left, double? height)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsFailure)
        {
            await Clients.Caller.SendAsync("Error", userIdResult.Error);
            return;
        }

        _cursorBroadcastScheduler.QueueDocumentCursor(
            documentId,
            Context.ConnectionId,
            userIdResult.Value,
            ResolveCursorDisplayName(displayName),
            offset,
            selectionEnd,
            top,
            left,
            height);
    }

    public async Task SyncContent(Guid documentId)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsFailure)
        {
            await Clients.Caller.SendAsync("Error", userIdResult.Error);
            return;
        }

        if (!await _authorizationService.CanAccessAsync(userIdResult.Value, documentId, AccessLevel.Editor))
        {
            await Clients.Caller.SendAsync("Error", "You do not have permission to edit this document.");
            return;
        }
        await Clients.Caller.SendAsync("Error", SnapshotSyncDeprecatedError);
        await SendTransportModeToCaller();
        await SendSnapshotResyncToCaller(documentId);
    }

    public async Task SyncTitle(Guid documentId, string title)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsFailure)
        {
            await Clients.Caller.SendAsync("Error", userIdResult.Error);
            return;
        }

        if (!await _authorizationService.CanAccessAsync(userIdResult.Value, documentId, AccessLevel.Editor))
        {
            await Clients.Caller.SendAsync("Error", "You do not have permission to edit this document.");
            return;
        }

        var normalizedTitle = title ?? string.Empty;
        if (normalizedTitle.Length > 200)
        {
            normalizedTitle = normalizedTitle[..200];
        }

        await Clients.OthersInGroup(documentId.ToString()).SendAsync("TitleUpdated", normalizedTitle);
    }

    public async Task SaveDocument(Guid documentId)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsFailure)
        {
            await Clients.Caller.SendAsync("Error", userIdResult.Error);
            return;
        }

        if (!await _authorizationService.CanAccessAsync(userIdResult.Value, documentId, AccessLevel.Editor))
        {
            await Clients.Caller.SendAsync("Error", "You do not have permission to edit this document.");
            return;
        }

        // OT updates are already queued during SendOperation; Save explicitly forces an immediate flush.
        await _documentPersistenceQueue.FlushDocumentAsync(documentId, Context.ConnectionAborted);
    }

    public async Task StartTyping(Guid documentId)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsFailure)
        {
            await Clients.Caller.SendAsync("Error", userIdResult.Error);
            return;
        }

        await Clients.Group(documentId.ToString()).SendAsync("UserTyping", new { UserId = userIdResult.Value, IsTyping = true });
    }

    public async Task StopTyping(Guid documentId)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsFailure)
        {
            await Clients.Caller.SendAsync("Error", userIdResult.Error);
            return;
        }

        await Clients.Group(documentId.ToString()).SendAsync("UserTyping", new { UserId = userIdResult.Value, IsTyping = false });
    }

    private async Task FlushPendingDocumentPersistenceIfIdle(Guid documentId, CancellationToken cancellationToken = default)
    {
        var activeSessions = await _sessionService.GetSessionCountAsync(documentId);
        if (activeSessions == 0)
        {
            await _documentPersistenceQueue.FlushDocumentAsync(documentId, cancellationToken);
        }
    }

    private string ResolveCursorDisplayName(string requestedDisplayName)
    {
        // Never trust client-sent identity for authenticated collaboration.
        var claims = Context.User;
        var serverIdentity = claims?.FindFirst(ClaimTypes.Name)?.Value
            ?? claims?.FindFirst("name")?.Value
            ?? claims?.FindFirst(ClaimTypes.Email)?.Value
            ?? claims?.FindFirst("email")?.Value;

        var normalizedServerIdentity = serverIdentity?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedServerIdentity))
        {
            return normalizedServerIdentity;
        }

        var normalizedRequested = requestedDisplayName?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedRequested))
        {
            return normalizedRequested;
        }

        return "User";
    }

    private async Task SendSnapshotResyncToCaller(Guid documentId)
    {
        var latestContent = await _sessionService.GetDocumentContentAsync(documentId) ?? string.Empty;
        var latestVersion = await _sessionService.GetVersionAsync(documentId);
        await Clients.Caller.SendAsync("ReceiveContent", ContentSyncEnvelopeCodec.SerializeSnapshot(latestContent));
        await Clients.Caller.SendAsync("ReceiveVersion", (int)latestVersion);
    }

    private async Task SendTransportModeToCaller()
    {
        await Clients.Caller.SendAsync("ReceiveTransportMode", new { OtEnabled = true });
    }

}

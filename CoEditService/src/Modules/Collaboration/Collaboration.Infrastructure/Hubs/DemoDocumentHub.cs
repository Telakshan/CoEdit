using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Collaboration.Application.Commands;
using Collaboration.Domain.Abstract;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Collaboration.Infrastructure.Hubs;

[AllowAnonymous]
[ExcludeFromCodeCoverage(Justification = "Lightweight demo hub; tested via E2E tests")]
public class DemoDocumentHub : Hub
{
    private const string SnapshotSyncDeprecatedError =
        "Snapshot content sync is no longer supported. Clients must use OT SendOperation.";

    private readonly ICursorBroadcastScheduler _cursorBroadcastScheduler;
    private readonly ISessionStateService _sessionStateService;
    private readonly ISender _sender;

    public DemoDocumentHub(
        ICursorBroadcastScheduler cursorBroadcastScheduler,
        ISessionStateService sessionStateService,
        ISender sender)
    {
        _cursorBroadcastScheduler = cursorBroadcastScheduler;
        _sessionStateService = sessionStateService;
        _sender = sender;
    }

    public async Task JoinDocument(string documentId)
    {
        var resolvedDocumentId = ResolveDocumentId(documentId);
        await Groups.AddToGroupAsync(Context.ConnectionId, documentId);
        var content = await _sessionStateService.GetDocumentContentAsync(resolvedDocumentId) ?? string.Empty;
        var version = await _sessionStateService.GetVersionAsync(resolvedDocumentId);
        await Clients.Caller.SendAsync("ReceiveContent", ContentSyncEnvelopeCodec.SerializeSnapshot(content));
        await Clients.Caller.SendAsync("ReceiveVersion", (int)version);
        await Clients.Caller.SendAsync("ReceiveTransportMode", new { OtEnabled = true });
        await Clients.Group(documentId).SendAsync("UserJoined", new { ConnectionId = Context.ConnectionId });
    }

    public async Task LeaveDocument(string documentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, documentId);
        await Clients.Group(documentId).SendAsync("UserLeft", new { ConnectionId = Context.ConnectionId });
    }

    public async Task RequestDocumentState(string documentId)
    {
        await SendSnapshotResyncToCaller(documentId);
        await Clients.Caller.SendAsync("ReceiveTransportMode", new { OtEnabled = true });
    }

    public async Task SendOperation(string documentId, OperationDto operationDto)
    {
        var resolvedDocumentId = ResolveDocumentId(documentId);
        var resolvedUserId = ResolveUserId(Context.ConnectionId);

        var commandDto = new OperationDto
        {
            DocumentId = resolvedDocumentId,
            UserId = resolvedUserId,
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
            var rejectionPayload = OperationRejectionPayloadFactory.Create(resolvedDocumentId, commandDto, result.Error);
            await Clients.Caller.SendAsync("ReceiveOperationRejected", rejectionPayload);
            await Clients.Caller.SendAsync("Error", rejectionPayload.Error);
            if (rejectionPayload.RequiresResync)
            {
                await SendSnapshotResyncToCaller(documentId);
            }
            return;
        }

        await Clients.Caller.SendAsync("ReceiveOperationAck", result.Value);
        await Clients.OthersInGroup(documentId).SendAsync("ReceiveOperation", result.Value);
    }

    public async Task SyncContent(string documentId, string _htmlContent)
    {
        await Clients.Caller.SendAsync("Error", SnapshotSyncDeprecatedError);
        await Clients.Caller.SendAsync("ReceiveTransportMode", new { OtEnabled = true });
        await SendSnapshotResyncToCaller(documentId);
    }

    public Task SaveDocument(string documentId)
    {
        // Demo mode is ephemeral; OT operations are applied immediately in Redis session state.
        return Task.CompletedTask;
    }

    public async Task SyncTitle(string documentId, string title)
    {
        var normalizedTitle = title ?? string.Empty;
        if (normalizedTitle.Length > 200)
        {
            normalizedTitle = normalizedTitle[..200];
        }

        await Clients.OthersInGroup(documentId).SendAsync("TitleUpdated", normalizedTitle);
    }

    public async Task UpdateCursor(string documentId, string displayName, int offset, int? selectionEnd, double? top, double? left, double? height)
    {
        _cursorBroadcastScheduler.QueueDemoCursor(
            documentId,
            Context.ConnectionId,
            displayName,
            offset,
            selectionEnd,
            top,
            left,
            height);

        await Task.CompletedTask;
    }

    private async Task SendSnapshotResyncToCaller(string documentId)
    {
        var resolvedDocumentId = ResolveDocumentId(documentId);
        var latestContent = await _sessionStateService.GetDocumentContentAsync(resolvedDocumentId) ?? string.Empty;
        var latestVersion = await _sessionStateService.GetVersionAsync(resolvedDocumentId);
        await Clients.Caller.SendAsync("ReceiveContent", ContentSyncEnvelopeCodec.SerializeSnapshot(latestContent));
        await Clients.Caller.SendAsync("ReceiveVersion", (int)latestVersion);
    }

    private static Guid ResolveDocumentId(string documentId)
    {
        if (Guid.TryParse(documentId, out var parsed) && parsed != Guid.Empty)
        {
            return parsed;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"demo-doc:{documentId}"));
        var guidBytes = new byte[16];
        Array.Copy(bytes, guidBytes, 16);
        var resolved = new Guid(guidBytes);
        return resolved == Guid.Empty
            ? Guid.Parse("11111111-1111-1111-1111-111111111111")
            : resolved;
    }

    private static Guid ResolveUserId(string connectionId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"demo-user:{connectionId}"));
        var guidBytes = new byte[16];
        Array.Copy(bytes, guidBytes, 16);
        var resolved = new Guid(guidBytes);
        return resolved == Guid.Empty
            ? Guid.Parse("22222222-2222-2222-2222-222222222222")
            : resolved;
    }

}

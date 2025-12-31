using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;

namespace CoEdit.Modules.Collaboration.Engine;

public class CollaborationHub: Hub
{
    private static readonly ConcurrentDictionary<string, HashSet<string>> _documentClients = new();
    private static readonly ConcurrentDictionary<string, string> _clientDocuments = new();
    private static readonly ConcurrentDictionary<string, List<string>> _documentHistory = new();

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[SignalR] Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        Console.WriteLine($"[SignalR] Client disconnected: {connectionId}");

        if (_clientDocuments.TryRemove(connectionId, out var documentId))
        {
            await LeaveDocumentInternal(documentId, connectionId);
        }
    }

    public async Task JoinDocument(string documentId)
    {
        var connectionId = Context.ConnectionId;
        Console.WriteLine($"[SignalR] Client {connectionId} joining document: {documentId}");

        _clientDocuments[connectionId] = documentId;

        _documentClients.AddOrUpdate(documentId,
            _ => [connectionId],
            (_, clients) =>
            {
                lock (clients) { clients.Add(connectionId); }
                return clients;
            });

        await Groups.AddToGroupAsync(connectionId, documentId);

        if (_documentClients.TryGetValue(documentId, out var history))
        {
            foreach (string delta in history)
            {
                await Clients.Caller.SendAsync("ReceiveDelta", delta);
            }
        }

        await Clients.OthersInGroup(documentId).SendAsync("UserJoined", connectionId);

        await Clients.Caller.SendAsync("JoinedDocument", documentId);
    }

    private async Task LeaveDocumentInternal(string documentId, string connectionId)
    {
        Console.WriteLine($"[SignalR] Client {connectionId} leaving document {documentId}");

        if (_documentClients.TryGetValue(documentId, out var clients))
        {
            lock (clients) { clients.Remove(connectionId); }
        }

        await Groups.RemoveFromGroupAsync(connectionId, documentId);

        await Clients.OthersInGroup(documentId)
            .SendAsync("UserLeft", connectionId);
    }
}

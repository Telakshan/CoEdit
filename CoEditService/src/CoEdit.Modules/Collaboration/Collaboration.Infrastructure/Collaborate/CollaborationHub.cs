using System;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoEdit.Modules.Collaboration.Engine;

public class CollaborationHub(DeltaBuffer deltaBuffer) : Hub
{
    private static readonly ConcurrentDictionary<string, HashSet<string>> _documentClients = new();
    private static readonly ConcurrentDictionary<string, string> _clientDocuments = new();
    private static readonly ConcurrentDictionary<string, List<string>> _documentHistory = new();
    
    private readonly DeltaBuffer _deltaBuffer = deltaBuffer;

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
    
        _documentHistory.TryAdd(documentId, new List<string>());
        
        await Groups.AddToGroupAsync(connectionId, documentId);

        if (_documentHistory.TryGetValue(documentId, out var history))
        {
            List<string> historySnapshot;
            lock (history) 
            {
                historySnapshot = history.ToList(); 
            }

            foreach (var delta in historySnapshot)
            {
                await Clients.Caller.SendAsync("ReceiveDelta", delta);
            }
        }

        await Clients.OthersInGroup(documentId).SendAsync("UserJoined", connectionId);

        await Clients.Caller.SendAsync("JoinedDocument", documentId);
    }

    public async Task SendDelta(string delta)
    {
        var connectionId = Context.ConnectionId;
        if (_clientDocuments.TryGetValue(connectionId, out var documentId))
        {
            if (_documentHistory.TryGetValue(documentId, out var history))
            {
                lock (history) { history.Add(delta); }
            }

            //Broadcast to others
            await Clients.OthersInGroup(documentId).SendAsync("ReceiveDelta", delta);
        }
    }

    private async Task LeaveDocumentInternal(string documentId, string connectionId)
    {
        bool isLastUser = false;
        
        Console.WriteLine($"[SignalR] Client {connectionId} leaving document {documentId}");

        if (_documentClients.TryGetValue(documentId, out var clients))
        {
            lock (clients)
            {
                clients.Remove(connectionId);
                if (clients.Count == 0)
                {
                    isLastUser = true;
                }
            }
        }

        await Groups.RemoveFromGroupAsync(connectionId, documentId);

        if (!isLastUser)
        {
            await Clients.OthersInGroup(documentId).SendAsync("UserLeft", connectionId);
        }
        
        if (isLastUser)
        {
            Console.WriteLine($"[SignalR] Last user left {documentId}. Offloading to persistence thread.");

            if (_documentHistory.TryRemove(documentId, out var finalHistory))
            {
                _documentClients.TryRemove(documentId, out _);
                
                await _deltaBuffer.WriteAsync(new DocumentSessionData(documentId, finalHistory));
            }
        }
    }
}

using System.Text.Json;
using Collaboration.Domain.Abstract;
using Collaboration.Domain.Entities;
using StackExchange.Redis;

namespace Collaboration.Infrastructure.Services;

public class SessionStateService(IConnectionMultiplexer redis) : ISessionStateService
{
    private IDatabase Db => redis.GetDatabase();

    public async Task AddSessionAsync(EditSession session)
    {
        var key = $"doc:{session.DocumentId}:sessions";
        var value = JsonSerializer.Serialize(session);
        
        await Db.HashSetAsync(key, session.ConnectionId, value);

        await Db.KeyExpireAsync(key, TimeSpan.FromHours(24));
    }

    public async Task RemoveSessionAsync(Guid documentId, string connectionId)
    {
        var key = $"doc:{documentId}:sessions";
        await Db.HashDeleteAsync(key, connectionId);    
        
        await Db.HashDeleteAsync($"doc:{documentId}:cursors", connectionId);
    }

    public async Task UpdateCursorAsync(Guid documentId, string connectionId, object cursorData)
    {
        var key = $"doc:{documentId}:cursors";
        var value = JsonSerializer.Serialize(cursorData);
        await Db.HashSetAsync(key, connectionId, value);
    }

    public async Task<List<string>> GetSessionsAsync(Guid documentId)
    {
        var key = $"doc:{documentId}:sessions";
        var entries = await Db.HashGetAllAsync(key);
        return entries.Select(x => x.Value.ToString()).ToList();
    }

    public async Task<long> IncrementVersionAsync(Guid documentId)
    {
        return await Db.StringIncrementAsync($"doc:{documentId}:version");
    }

    public async Task<long> GetVersionAsync(Guid documentId)
    {
        var val = await Db.StringGetAsync($"doc:{documentId}:version");
        return val.HasValue ? (long)val : 0;    
    }

    public async Task AddOperationAsync(Operation op)
    {
        var key = $"doc:{op.DocumentId}:ops";
        var value = JsonSerializer.Serialize(op);
        await Db.ListRightPushAsync(key, value);   
    }

    public async Task<List<Operation>> GetOperationsAfterVersionAsync(Guid documentId, long version)
    {
        var key = $"doc:{documentId}:ops";
        
        var ops = await Db.ListRangeAsync(key, version, -1);

        return ops.Select(opJson => JsonSerializer.Deserialize<Operation>(opJson.ToString())).OfType<Operation>().ToList();
    }
}
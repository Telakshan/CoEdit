using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Collaboration.Application.Services;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Operations;
using Collaboration.Infrastructure.ValueObjects;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Collaboration.Infrastructure.Services;

[ExcludeFromCodeCoverage(Justification = "Redis-backed operation queue requiring live Redis connection")]
internal class RedisOperationManager : IOperationManager
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IOperationalTransform _transform;
    private readonly ILogger<RedisOperationManager> _logger;

    public RedisOperationManager(
        IConnectionMultiplexer redis,
        IOperationalTransform transform,
        ILogger<RedisOperationManager> logger)
    {
        _redis = redis;
        _transform = transform;
        _logger = logger;
    }

    public async Task<long> GetNextVersionAsync(Guid documentId)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeyConstants.DocumentVersion(documentId);
        return await db.StringIncrementAsync(key);
    }

    public async Task<long> GetCurrentVersionAsync(Guid documentId)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeyConstants.DocumentVersion(documentId);
        var val = await db.StringGetAsync(key);
        return val.HasValue ? (long)val : 0;
    }

    public async Task QueueOperationAsync(Guid documentId, Operation operation)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeyConstants.DocumentOperations(documentId);

        var value = JsonSerializer.Serialize(operation);
        await db.ListRightPushAsync(key, value);
    }

    public async Task<IEnumerable<Operation>> GetPendingOperationsAsync(Guid documentId, long afterVersion)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeyConstants.DocumentOperations(documentId);

        var ops = new List<Operation>();
        const int BatchSize = 250;
        var listLength = await db.ListLengthAsync(key);
        
        for (int i = 0; i < listLength; i += BatchSize)
        {
            var items = await db.ListRangeAsync(key, i, i + BatchSize - 1);
            if (items.Length == 0) break;

            foreach (var item in items)
            {
                try
                {
                    var op = JsonSerializer.Deserialize<Operation>((string)item!);
                    if (op != null && op.Version > afterVersion)
                    {
                        ops.Add(op);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Skipping malformed operation record during GetPendingOperationsAsync");
                }
            }
        }

        return ops;
    }

    public async Task<Operation> ProcessOperationAsync(Guid documentId, Operation operation, long clientBaseVersion)
    {
        var concurrentOps = await GetPendingOperationsAsync(documentId, clientBaseVersion);

        var othersConcurrentOps = concurrentOps.Where(op => op.UserId != operation.UserId).OrderBy(op => op.Version);

        var transformedOps = _transform.TransformAgainstConcurrent(operation, othersConcurrentOps);

        return transformedOps.FirstOrDefault() ?? operation;
    }
}

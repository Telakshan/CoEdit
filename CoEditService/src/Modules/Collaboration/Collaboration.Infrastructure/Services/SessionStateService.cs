using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Collaboration.Domain.Abstract;
using Collaboration.Domain.Entities;
using Collaboration.Infrastructure.Resilience;
using Collaboration.Infrastructure.ValueObjects;
using StackExchange.Redis;

namespace Collaboration.Infrastructure.Services;

[ExcludeFromCodeCoverage(Justification = "Redis-backed session state requiring live Redis connection")]
internal class SessionStateService : ISessionStateService
{
    private readonly IDatabase _db;
    private readonly ResiliencePipeline _retryPipeline;

    // Max operations retained per document in Redis.
    // Clients further behind than this must do a full resync.
    private const int MaxOpsPerDocument = 1000;
    // Keep a larger recent-operation id index for retry idempotency even if the op list is compacted.
    private const int MaxTrackedOperationIdsPerDocument = 4000;
    private static readonly TimeSpan SessionStateTtl = TimeSpan.FromHours(24);

    public SessionStateService(
        IConnectionMultiplexer redis,
        ResiliencePipelineProvider<string> pipelineProvider)
    {
        _db = redis.GetDatabase();
        _retryPipeline = pipelineProvider.GetPipeline(ResiliencePipelineNames.Redis);
    }

    public async Task AddSessionAsync(EditSession session)
    {
        await _retryPipeline.ExecuteAsync(async ct =>
        {
            var key = RedisKeyConstants.DocumentSessions(session.DocumentId);
            var connectionDocsKey = RedisKeyConstants.ConnectionDocuments(session.ConnectionId);
            var value = JsonSerializer.Serialize(session);
            await _db.SetAddAsync(connectionDocsKey, session.DocumentId.ToString());
            // Store as Hash: Field = ConnectionId, Value = SessionData
            await _db.HashSetAsync(key, session.ConnectionId, value);

            // Set expiry on keys to clean up if no activity
            await _db.KeyExpireAsync(key, SessionStateTtl);
            await _db.KeyExpireAsync(connectionDocsKey, SessionStateTtl);
        });
    }

    public async Task RemoveSessionAsync(Guid documentId, string connectionId)
    {
        await _retryPipeline.ExecuteAsync(async ct =>
        {
            var key = RedisKeyConstants.DocumentSessions(documentId);
            var cursorKey = RedisKeyConstants.DocumentCursors(documentId);
            var connectionDocsKey = RedisKeyConstants.ConnectionDocuments(connectionId);
            await _db.HashDeleteAsync(key, connectionId);

            // Also remove cursor
            await _db.HashDeleteAsync(cursorKey, connectionId);

            // Keep reverse index in sync for explicit leave flows.
            await _db.SetRemoveAsync(connectionDocsKey, documentId.ToString());
        });
    }

    public async Task<IReadOnlyList<Guid>> RemoveSessionFromAllDocumentsAsync(string connectionId)
    {
        return await _retryPipeline.ExecuteAsync(async ct =>
        {
            var connectionDocsKey = RedisKeyConstants.ConnectionDocuments(connectionId);
            var affectedDocuments = new HashSet<Guid>();
            var indexedDocumentIds = await _db.SetMembersAsync(connectionDocsKey);

            foreach (var indexedDocumentId in indexedDocumentIds)
            {
                var indexedValue = indexedDocumentId.ToString();
                if (!Guid.TryParse(indexedValue, out var documentId))
                {
                    continue;
                }

                var removed = await _db.HashDeleteAsync(RedisKeyConstants.DocumentSessions(documentId), connectionId);
                await _db.HashDeleteAsync(RedisKeyConstants.DocumentCursors(documentId), connectionId);

                if (removed)
                {
                    affectedDocuments.Add(documentId);
                }
            }

            await _db.KeyDeleteAsync(connectionDocsKey);
            return (IReadOnlyList<Guid>)affectedDocuments.ToArray();
        });
    }

    public async Task UpdateCursorAsync(Guid documentId, string connectionId, object cursorData)
    {
        await _retryPipeline.ExecuteAsync(async ct =>
        {
            var key = RedisKeyConstants.DocumentCursors(documentId);
            var value = JsonSerializer.Serialize(cursorData);
            await _db.HashSetAsync(key, connectionId, value);
        });
    }

    public async Task<long> GetSessionCountAsync(Guid documentId)
    {
        return await _retryPipeline.ExecuteAsync(async ct =>
            await _db.HashLengthAsync(RedisKeyConstants.DocumentSessions(documentId)));
    }

    public async Task<List<string>> GetSessionsAsync(Guid documentId)
    {
        return await _retryPipeline.ExecuteAsync(async ct =>
        {
            var key = RedisKeyConstants.DocumentSessions(documentId);
            var entries = await _db.HashGetAllAsync(key);
            return entries.Select(x => x.Value.ToString()).ToList();
        });
    }

    public async Task<long> IncrementVersionAsync(Guid documentId)
    {
        return await _db.StringIncrementAsync(RedisKeyConstants.DocumentVersion(documentId));
    }

    public async Task<long> GetVersionAsync(Guid documentId)
    {
        return await _retryPipeline.ExecuteAsync(async ct =>
        {
            var val = await _db.StringGetAsync(RedisKeyConstants.DocumentVersion(documentId));
            return val.HasValue ? (long)val : 0;
        });
    }

    // Simple Op Queue
    public async Task AddOperationAsync(Operation op)
    {
        var key = RedisKeyConstants.DocumentOperations(op.DocumentId);
        var value = JsonSerializer.Serialize(op);
        await _db.ListRightPushAsync(key, value);
        // Trim to keep only the most recent operations, preventing unbounded growth
        await _db.ListTrimAsync(key, -MaxOpsPerDocument, -1);

        // Renew session TTL on every operation so active documents don't silently expire.
        await _db.KeyExpireAsync(RedisKeyConstants.DocumentSessions(op.DocumentId), SessionStateTtl);

        if (op.OperationId == Guid.Empty)
        {
            return;
        }

        var operationId = op.OperationId.ToString("N");
        var operationByIdKey = RedisKeyConstants.DocumentOpById(op.DocumentId);
        var operationIdIndexKey = RedisKeyConstants.DocumentOpIdIndex(op.DocumentId);
        await _db.HashSetAsync(operationByIdKey, operationId, value);
        await _db.SortedSetAddAsync(operationIdIndexKey, operationId, op.Version);
        await _db.KeyExpireAsync(operationByIdKey, SessionStateTtl);
        await _db.KeyExpireAsync(operationIdIndexKey, SessionStateTtl);

        var pruneBeforeVersion = op.Version - MaxTrackedOperationIdsPerDocument;
        if (pruneBeforeVersion <= 0)
        {
            return;
        }

        var staleOperationIds = await _db.SortedSetRangeByScoreAsync(
            operationIdIndexKey,
            double.NegativeInfinity,
            pruneBeforeVersion);

        if (staleOperationIds.Length == 0)
        {
            return;
        }

        await _db.HashDeleteAsync(operationByIdKey, staleOperationIds);
        await _db.SortedSetRemoveAsync(operationIdIndexKey, staleOperationIds);
    }

    public async Task<Operation?> GetOperationByIdAsync(Guid documentId, Guid operationId)
    {
        if (operationId == Guid.Empty)
        {
            return null;
        }

        return await _retryPipeline.ExecuteAsync(async ct =>
        {
            var raw = await _db.HashGetAsync(RedisKeyConstants.DocumentOpById(documentId), operationId.ToString("N"));
            if (!raw.HasValue)
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<Operation>(raw.ToString());
            }
            catch (JsonException)
            {
                return null;
            }
        });
    }

    public async Task<List<Operation>> GetOperationsAfterVersionAsync(Guid documentId, long version)
    {
        return await _retryPipeline.ExecuteAsync(async ct =>
        {
            var key = RedisKeyConstants.DocumentOperations(documentId);

            // The list is trimmed to MaxOpsPerDocument entries.
            // We need operations from 'version' onwards. Since trimming removes the oldest
            // entries, the list index no longer maps 1:1 to version numbers.
            // Fetch all remaining ops (bounded by MaxOpsPerDocument) and filter by version.
            var ops = await _db.ListRangeAsync(key, 0, -1);

            var result = new List<Operation>();
            foreach (var opJson in ops)
            {
                try
                {
                    var op = JsonSerializer.Deserialize<Operation>(opJson.ToString());
                    if (op != null && op.Version > version)
                    {
                        result.Add(op);
                    }
                }
                catch (JsonException)
                {
                    // Skip malformed op records so one bad payload cannot break replay reads.
                }
            }
            return result;
        });
    }

    public async Task SetDocumentContentAsync(Guid documentId, string content)
    {
        await _retryPipeline.ExecuteAsync(async ct =>
        {
            var key = RedisKeyConstants.DocumentContent(documentId);
            await _db.StringSetAsync(key, content, SessionStateTtl);
        });
    }

    public async Task<string?> GetDocumentContentAsync(Guid documentId)
    {
        return await _retryPipeline.ExecuteAsync(async ct =>
        {
            var key = RedisKeyConstants.DocumentContent(documentId);
            var value = await _db.StringGetAsync(key);
            return value.HasValue ? value.ToString() : (string?)null;
        });
    }

    public async Task<List<Operation>> GetOperationsAsync(Guid documentId)
    {
        return await GetOperationsPaginatedAsync(documentId, 0, -1);
    }

    public async Task<List<Operation>> GetOperationsPaginatedAsync(Guid documentId, int skip, int take)
    {
        return await _retryPipeline.ExecuteAsync(async ct =>
        {
            var key = RedisKeyConstants.DocumentOperations(documentId);
            // Redis ListRange: stop index is inclusive. If take is 10, stop should be skip + 9.
            // If take is -1, it means fetch until the end.
            var stop = take == -1 ? -1 : skip + take - 1;
            var ops = await _db.ListRangeAsync(key, skip, stop);

            var result = new List<Operation>();
            foreach (var opJson in ops)
            {
                try
                {
                    var op = JsonSerializer.Deserialize<Operation>(opJson.ToString());
                    if (op != null)
                    {
                        result.Add(op);
                    }
                }
                catch (JsonException)
                {
                }
            }
            return result;
        });
    }

    public async Task<bool> CommitCompactionAsync(Guid documentId, string newContent, long expectedVersion, int retainedOps)
    {
        return await _retryPipeline.ExecuteAsync(async ct =>
        {
            var opsKey = RedisKeyConstants.DocumentOperations(documentId);
            var contentKey = RedisKeyConstants.DocumentContent(documentId);
            var versionKey = RedisKeyConstants.DocumentVersion(documentId);
            var cursorsKey = RedisKeyConstants.DocumentCursors(documentId);

            var transaction = _db.CreateTransaction();

            // Guard: abort if a concurrent operation incremented the version since we read it.
            // This prevents version regression and ensures no ops added during compaction are lost.
            transaction.AddCondition(Condition.StringEqual(versionKey, expectedVersion.ToString()));

            // Update content and version
            _ = transaction.StringSetAsync(contentKey, newContent, SessionStateTtl);
            _ = transaction.StringSetAsync(versionKey, expectedVersion.ToString());

            // Trim operations
            if (retainedOps <= 0)
            {
                _ = transaction.KeyDeleteAsync(opsKey);
            }
            else
            {
                _ = transaction.ListTrimAsync(opsKey, -retainedOps, -1);
            }

            // Cleanup stale cursors
            _ = transaction.KeyDeleteAsync(cursorsKey);

            return await transaction.ExecuteAsync();
        });
    }

    public async Task ClearDocumentStateAsync(Guid documentId)
    {
        await _retryPipeline.ExecuteAsync(async ct =>
        {
            var sessionsKey = RedisKeyConstants.DocumentSessions(documentId);
            var activeConnectionIds = await _db.HashKeysAsync(sessionsKey);
            foreach (var activeConnectionId in activeConnectionIds)
            {
                var connectionId = activeConnectionId.ToString();
                if (string.IsNullOrWhiteSpace(connectionId))
                {
                    continue;
                }

                await _db.SetRemoveAsync(RedisKeyConstants.ConnectionDocuments(connectionId), documentId.ToString());
            }

            RedisKey[] keys =
            [
                sessionsKey,
                RedisKeyConstants.DocumentCursors(documentId),
                RedisKeyConstants.DocumentOperations(documentId),
                RedisKeyConstants.DocumentOpById(documentId),
                RedisKeyConstants.DocumentOpIdIndex(documentId),
                RedisKeyConstants.DocumentVersion(documentId),
                RedisKeyConstants.DocumentContent(documentId)
            ];

            await _db.KeyDeleteAsync(keys);
        });
    }
}
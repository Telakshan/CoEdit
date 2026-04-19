using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Collaboration.Application.Services;
using Collaboration.Domain.Entities;
using Collaboration.Infrastructure.ValueObjects;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Collaboration.Infrastructure.Services;

[ExcludeFromCodeCoverage(Justification = "Redis-backed session storage requiring live Redis connection")]
internal class RedisDocumentSessionManager : IDocumentSessionManager
{
    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(24);

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDocumentSessionManager> _logger;

    public RedisDocumentSessionManager(IConnectionMultiplexer redis, ILogger<RedisDocumentSessionManager> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<EditSession> JoinDocumentAsync(
        Guid documentId,
        Guid userId,
        string connectionId,
        string displayName)
    {
        var db = _redis.GetDatabase();
        var session = new EditSession(documentId, userId, connectionId)
        {
            DisplayName = displayName,
            UserColor = AssignColor(userId, documentId)
        };

        var key = RedisKeyConstants.DocumentSessions(documentId);
        var score = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var value = JsonSerializer.Serialize(session);

        await db.SortedSetAddAsync(key, value, score);

        await db.KeyExpireAsync(key, SessionTtl);

        return session;
    }

    public async Task LeaveDocumentAsync(Guid documentId, Guid userId, string connectionId)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeyConstants.DocumentSessions(documentId);

        var sessions = await db.SortedSetRangeByRankAsync(key);
        foreach (var sessionJson in sessions)
        {
            try
            {
                var session = JsonSerializer.Deserialize<EditSession>((string)sessionJson!);
                if (session != null && session.UserId == userId && session.ConnectionId == connectionId)
                {
                    await db.SortedSetRemoveAsync(key, sessionJson);
                    break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Skipping malformed session record in LeaveDocumentAsync for document {DocumentId}", documentId);
            }
        }
    }

    public async Task<IEnumerable<EditSession>> GetActiveSessionsAsync(Guid documentId)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeyConstants.DocumentSessions(documentId);
        var sessions = await db.SortedSetRangeByRankAsync(key);

        var result = new List<EditSession>();
        foreach (var s in sessions)
        {
            if (s.HasValue)
            {
                try
                {
                    var session = JsonSerializer.Deserialize<EditSession>((string)s!);
                    if (session != null) result.Add(session);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Skipping malformed session record in GetActiveSessionsAsync for document {DocumentId}", documentId);
                }
            }
        }
        return result;
    }

    public async Task UpdateActivityAsync(Guid documentId, Guid userId)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeyConstants.DocumentSessions(documentId);

        var sessions = await db.SortedSetRangeByRankAsync(key);
        EditSession? targetSession = null;
        RedisValue targetValue = RedisValue.Null;

        foreach (var s in sessions)
        {
            try
            {
                var session = JsonSerializer.Deserialize<EditSession>((string)s!);
                if (session != null && session.UserId == userId)
                {
                    targetSession = session;
                    targetValue = s;
                    break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Skipping malformed session record in UpdateActivityAsync for document {DocumentId}", documentId);
            }
        }

        if (targetSession != null)
        {
            await db.SortedSetRemoveAsync(key, targetValue);

            targetSession.UpdateActivity();
            var newValue = JsonSerializer.Serialize(targetSession);
            var newScore = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            await db.SortedSetAddAsync(key, newValue, newScore);
            await db.KeyExpireAsync(key, SessionTtl);
        }
    }

    public async Task<bool> IsUserInSessionAsync(Guid documentId, Guid userId)
    {
        var sessions = await GetActiveSessionsAsync(documentId);
        return sessions.Any(s => s.UserId == userId);
    }

    private string AssignColor(Guid userId, Guid documentId)
    {
        var hash = userId.GetHashCode() ^ documentId.GetHashCode();
        var colors = new[] { "#FF5733", "#33FF57", "#3357FF", "#F033FF", "#FF33A6", "#33FFF5", "#FFC733" };
        return colors[Math.Abs(hash) % colors.Length];
    }
}
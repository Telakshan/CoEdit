using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Collaboration.Infrastructure.Services;

[ExcludeFromCodeCoverage(Justification = "BackgroundService with Redis scan; requires live Redis and timing control")]
public class SessionCleanupService(ILogger<SessionCleanupService> logger, IConnectionMultiplexer redis)
    : BackgroundService
{
    private const string ActiveDocumentsSetKey = "active_documents";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredSessionsAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error cleaning up sessions");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CleanupExpiredSessionsAsync()
    {
        var db = redis.GetDatabase();
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds();
        
        const string script = @"
            redis.call('ZREMRANGEBYSCORE', KEYS[1], '-inf', ARGV[1])
            if redis.call('ZCARD', KEYS[1]) == 0 then
                redis.call('SREM', KEYS[2], KEYS[1])
            end";

        await foreach (var key in db.SetScanAsync(ActiveDocumentsSetKey))
        {
            var docKey = (string)key!;
            if (string.IsNullOrEmpty(docKey)) continue;

            await db.ScriptEvaluateAsync(script, keys: [docKey, ActiveDocumentsSetKey], values: [cutoff]);
        }
    }
}
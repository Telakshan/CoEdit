using System.Diagnostics.CodeAnalysis;
using CoEdit.Common.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoEdit.Common.Infrastructure.Outbox;


[ExcludeFromCodeCoverage(Justification = "BackgroundService for outbox cleanup; requires timing, scope management, and DB access")]
public class OutboxCleanupWorker<TDbContext>(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<OutboxCleanupOptions> options,
    ILogger<OutboxCleanupWorker<TDbContext>> logger)
    : BackgroundService
    where TDbContext : BaseDbContext
{
    private readonly OutboxCleanupOptions _options = options.Value;
    private bool _deadLetterTableEnsured;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cleanupInterval = TimeSpan.FromMinutes(Math.Max(1, _options.CleanupIntervalMinutes));
        var poisonScanInterval = TimeSpan.FromMinutes(Math.Max(1, _options.PoisonScanIntervalMinutes));
        var processorName = typeof(TDbContext).Name;

        var nextCleanupUtc = DateTime.UtcNow.Add(cleanupInterval);
        var nextPoisonScanUtc = DateTime.UtcNow.Add(poisonScanInterval);

        // Short initial delay to let the app finish starting up
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            try
            {
                if (now >= nextPoisonScanUtc)
                {
                    await QuarantinePoisonedMessagesAsync(processorName, stoppingToken);
                    nextPoisonScanUtc = DateTime.UtcNow.Add(poisonScanInterval);
                }

                if (now >= nextCleanupUtc)
                {
                    await PurgeProcessedMessagesAsync(processorName, stoppingToken);
                    nextCleanupUtc = DateTime.UtcNow.Add(cleanupInterval);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox cleanup worker {Processor} encountered an error.", processorName);
            }

            // Sleep until the next scheduled action
            var nextActionUtc = nextPoisonScanUtc < nextCleanupUtc ? nextPoisonScanUtc : nextCleanupUtc;
            var delay = nextActionUtc - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    private async Task PurgeProcessedMessagesAsync(string processorName, CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var schema = dbContext.Model.GetDefaultSchema() ?? "public";
        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, _options.RetentionDays));
        var batchSize = Math.Max(100, _options.DeleteBatchSize);
        long totalDeleted = 0;

        int deleted;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Use raw SQL for efficient batched deletes without loading entities into memory
            deleted = await dbContext.Database.ExecuteSqlRawAsync(
                $@"DELETE FROM ""{schema}"".""OutboxMessages""
                   WHERE ""Id"" IN (
                       SELECT ""Id"" FROM ""{schema}"".""OutboxMessages""
                       WHERE ""ProcessedAt"" IS NOT NULL AND ""ProcessedAt"" < {{0}}
                       LIMIT {{1}}
                   )",
                new object[] { cutoff, batchSize },
                cancellationToken);

            totalDeleted += deleted;
        } while (deleted >= batchSize);
    }

    private async Task QuarantinePoisonedMessagesAsync(string processorName, CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        await EnsureDeadLetterTableAsync(dbContext, cancellationToken);

        var schema = dbContext.Model.GetDefaultSchema() ?? "public";
        var maxRetryCount = Math.Max(1, _options.MaxRetryCount);
        var batchSize = Math.Max(10, _options.DeleteBatchSize);

        var poisonedMessages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount >= maxRetryCount)
            .OrderBy(m => m.SequenceNumber)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (poisonedMessages.Count == 0)
        {
            return;
        }

        var quarantinedAt = DateTime.UtcNow;

        foreach (var message in poisonedMessages)
        {
            // Log structured warning to Serilog -> Seq
            logger.LogWarning(
                "Outbox poisoned message detected. " +
                "MessageId={MessageId} EventType={EventType} SourceSchema={SourceSchema} " +
                "RetryCount={RetryCount} OccurredAt={OccurredAt} Error={Error}",
                message.Id,
                message.Type,
                schema,
                message.RetryCount,
                message.OccurredAt,
                message.Error);

            // Create dead letter entry
            var deadLetter = new OutboxDeadLetter
            {
                Id = message.Id,
                Type = message.Type,
                Content = message.Content,
                AssemblyQualifiedName = message.AssemblyQualifiedName,
                OccurredAt = message.OccurredAt,
                Error = message.Error,
                RetryCount = message.RetryCount,
                SourceSchema = schema,
                QuarantinedAt = quarantinedAt
            };

            dbContext.OutboxDeadLetters.Add(deadLetter);
            dbContext.OutboxMessages.Remove(message);
        }

        // Both insert dead letters and remove originals in one transaction
        await dbContext.SaveChangesAsync(cancellationToken);

    }

    private async Task EnsureDeadLetterTableAsync(TDbContext dbContext, CancellationToken cancellationToken)
    {
        if (_deadLetterTableEnsured || !dbContext.Database.IsRelational())
        {
            return;
        }

        var providerName = dbContext.Database.ProviderName ?? string.Empty;
        if (!providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            _deadLetterTableEnsured = true;
            return;
        }

        var schema = dbContext.Model.GetDefaultSchema() ?? "public";
        var sql = $@"
CREATE TABLE IF NOT EXISTS ""{schema}"".""OutboxDeadLetters"" (
    ""Id"" uuid NOT NULL PRIMARY KEY,
    ""Type"" text NOT NULL DEFAULT '',
    ""Content"" text NOT NULL,
    ""AssemblyQualifiedName"" text,
    ""OccurredAt"" timestamp with time zone NOT NULL,
    ""Error"" text,
    ""RetryCount"" integer NOT NULL DEFAULT 0,
    ""SourceSchema"" varchar(128) NOT NULL DEFAULT '',
    ""QuarantinedAt"" timestamp with time zone NOT NULL
);
CREATE INDEX IF NOT EXISTS ""IX_OutboxDeadLetters_Schema_Quarantined""
ON ""{schema}"".""OutboxDeadLetters"" (""SourceSchema"", ""QuarantinedAt"");";

        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure OutboxDeadLetters table for schema {Schema}.", schema);
        }
        finally
        {
            _deadLetterTableEnsured = true;
        }
    }
}
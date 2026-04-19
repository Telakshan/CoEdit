using System.Text.Json;
using System.Diagnostics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Diagnostics.CodeAnalysis;
using CoEdit.Common.Infrastructure.Data;
using CoEdit.Common.Infrastructure.Outbox;

namespace CoEdit.BuildingBlocks.Infrastructure.Outbox;

[ExcludeFromCodeCoverage(Justification = "BackgroundService for outbox processing; requires timing and scope management")]
public class OutboxProcessor<TDbContext> : BackgroundService
    where TDbContext : BaseDbContext
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<OutboxProcessor<TDbContext>> _logger;
    private readonly OutboxProcessorOptions _options;
    private DateTime _nextBacklogRefreshUtc = DateTime.MinValue;
    private bool _pollingIndexEnsured;

    public OutboxProcessor(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<OutboxProcessorOptions> options,
        ILogger<OutboxProcessor<TDbContext>> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consecutiveFailureCycles = 0;
        var consecutiveEmptyCycles = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cycleResult = await ProcessOutboxMessages(stoppingToken);

                if (cycleResult.HadMessages && cycleResult.DrainedCount == 0)
                {
                    consecutiveFailureCycles++;
                    consecutiveEmptyCycles = 0;
                    await Task.Delay(ApplyJitter(ComputeFailureDelay(consecutiveFailureCycles)), stoppingToken);
                    continue;
                }

                consecutiveFailureCycles = 0;

                if (cycleResult.Disposition == OutboxCycleDisposition.Empty)
                {
                    consecutiveEmptyCycles++;
                    await Task.Delay(ApplyJitter(ComputeEmptyDelay(consecutiveEmptyCycles)), stoppingToken);
                    continue;
                }

                consecutiveEmptyCycles = 0;

                if (cycleResult.Disposition is OutboxCycleDisposition.Partial or OutboxCycleDisposition.DrainedToEmpty)
                {
                    await Task.Delay(ApplyJitter(ComputePartialBatchDelay()), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                consecutiveFailureCycles++;
                consecutiveEmptyCycles = 0;
                _logger.LogError(ex, "Error occurred while processing outbox messages");
                await Task.Delay(ApplyJitter(ComputeFailureDelay(consecutiveFailureCycles)), stoppingToken);
            }
        }
    }

    private async Task<OutboxCycleResult> ProcessOutboxMessages(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var processorName = typeof(TDbContext).Name;
        var batchSize = Math.Max(1, _options.BatchSize);
        var maxRetryCount = Math.Max(1, _options.MaxRetryCount);
        var maxConsecutiveBatches = Math.Max(1, _options.MaxConsecutiveBatches);

        await EnsurePollingIndexAsync(dbContext, stoppingToken);

        var hadMessages = false;
        long drainedTotal = 0;
        var disposition = OutboxCycleDisposition.Empty;

        for (var batch = 1; batch <= maxConsecutiveBatches; batch++)
        {
            var stopwatch = Stopwatch.StartNew();
            var messages = await dbContext.OutboxMessages
                .Where(m => m.ProcessedAt == null && m.RetryCount < maxRetryCount)
                .OrderBy(m => m.SequenceNumber)
                .Take(batchSize)
                .ToListAsync(stoppingToken);

            if (messages.Count == 0)
            {
                await RefreshBacklogGaugeIfDueAsync(dbContext, processorName, maxRetryCount, stoppingToken);
                disposition = hadMessages
                    ? OutboxCycleDisposition.DrainedToEmpty
                    : OutboxCycleDisposition.Empty;
                break;
            }

            hadMessages = true;
            long drainedCount = 0;

            foreach (var message in messages)
            {
                try
                {
                    var type = Type.GetType(message.AssemblyQualifiedName!);
                    if (type == null)
                    {
                        _logger.LogError("Could not load type {Type}", message.Type);
                        message.ProcessedAt = DateTime.UtcNow;
                        message.Error = $"Could not load type {message.Type}";
                        drainedCount++;
                        continue;
                    }

                    var domainEvent = JsonSerializer.Deserialize(message.Content, type);
                    if (domainEvent == null)
                    {
                        _logger.LogError("Could not deserialize message {MessageId}", message.Id);
                        message.ProcessedAt = DateTime.UtcNow;
                        message.Error = "Deserialization failed";
                        drainedCount++;
                        continue;
                    }

                    if (domainEvent is INotification notification)
                    {
                        await publisher.Publish(notification, stoppingToken);
                    }

                    message.ProcessedAt = DateTime.UtcNow;
                    message.Error = null;
                    drainedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message {MessageId}", message.Id);
                    message.RetryCount++;
                    message.Error = ex.ToString();
                    // Exponential backoff by cycle is applied by the processor when drained count is zero.
                }
            }

            await dbContext.SaveChangesAsync(stoppingToken);

            stopwatch.Stop();
            drainedTotal += drainedCount;
            await RefreshBacklogGaugeIfDueAsync(dbContext, processorName, maxRetryCount, stoppingToken);

            if (messages.Count < batchSize)
            {
                disposition = OutboxCycleDisposition.Partial;
                break;
            }

            disposition = OutboxCycleDisposition.BacklogLikely;
        }

        return new OutboxCycleResult(hadMessages, drainedTotal, disposition);
    }

    private async Task RefreshBacklogGaugeIfDueAsync(
        TDbContext dbContext,
        string processorName,
        int maxRetryCount,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        if (now < _nextBacklogRefreshUtc)
        {
            return;
        }

        _ = await dbContext.OutboxMessages
            .LongCountAsync(m => m.ProcessedAt == null && m.RetryCount < maxRetryCount, cancellationToken);

        var refreshSeconds = Math.Max(1, _options.BacklogRefreshIntervalSeconds);
        _nextBacklogRefreshUtc = now.AddSeconds(refreshSeconds);
    }

    private async Task EnsurePollingIndexAsync(TDbContext dbContext, CancellationToken cancellationToken)
    {
        if (_pollingIndexEnsured || !dbContext.Database.IsRelational())
        {
            return;
        }

        var providerName = dbContext.Database.ProviderName ?? string.Empty;
        if (!providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            _pollingIndexEnsured = true;
            return;
        }

        var schema = dbContext.Model.GetDefaultSchema() ?? "public";
        var sql = $@"
CREATE INDEX IF NOT EXISTS ""IX_OutboxMessages_Polling""
ON ""{schema}"".""OutboxMessages"" (""RetryCount"", ""SequenceNumber"")
WHERE ""ProcessedAt"" IS NULL;";

        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to ensure outbox polling index for schema {Schema}.", schema);
        }
        finally
        {
            _pollingIndexEnsured = true;
        }
    }

    private TimeSpan ComputeEmptyDelay(int consecutiveEmptyCycles)
    {
        var baseDelayMs = Math.Max(50, _options.IdleDelayMs);
        var maxDelayMs = Math.Max(baseDelayMs, _options.MaxIdleDelayMs);
        var exponent = Math.Min(Math.Max(consecutiveEmptyCycles - 1, 0), 6);
        var delayMs = baseDelayMs * (1 << exponent);
        return TimeSpan.FromMilliseconds(Math.Min(delayMs, maxDelayMs));
    }

    private TimeSpan ComputePartialBatchDelay()
    {
        var partialDelayMs = Math.Max(25, _options.PartialBatchDelayMs);
        return TimeSpan.FromMilliseconds(partialDelayMs);
    }

    private TimeSpan ComputeFailureDelay(int consecutiveFailureCycles)
    {
        var baseDelayMs = Math.Max(50, _options.ErrorDelayMs);
        var maxDelayMs = Math.Max(baseDelayMs, _options.MaxErrorDelayMs);
        var exponent = Math.Min(Math.Max(consecutiveFailureCycles - 1, 0), 6);
        var delayMs = baseDelayMs * (1 << exponent);
        return TimeSpan.FromMilliseconds(Math.Min(delayMs, maxDelayMs));
    }

    private TimeSpan ApplyJitter(TimeSpan baseDelay)
    {
        var jitterMaxMs = Math.Max(0, _options.JitterMaxMs);
        if (jitterMaxMs == 0 || baseDelay <= TimeSpan.Zero)
        {
            return baseDelay;
        }

        var jitterMs = Random.Shared.Next(0, jitterMaxMs + 1);
        return baseDelay + TimeSpan.FromMilliseconds(jitterMs);
    }

    private readonly record struct OutboxCycleResult(
        bool HadMessages,
        long DrainedCount,
        OutboxCycleDisposition Disposition);

    private enum OutboxCycleDisposition
    {
        Empty = 0,
        Partial = 1,
        DrainedToEmpty = 2,
        BacklogLikely = 3
    }
}

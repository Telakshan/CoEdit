namespace CoEdit.Common.Infrastructure.Outbox;

public sealed class OutboxProcessorOptions
{
    public const string SectionName = "OutboxProcessor";

    public int BatchSize { get; set; } = 100;
    public int MaxRetryCount { get; set; } = 3;
    public int IdleDelayMs { get; set; } = 250;
    public int MaxIdleDelayMs { get; set; } = 5000;
    public int PartialBatchDelayMs { get; set; } = 150;
    public int JitterMaxMs { get; set; } = 200;
    public int ErrorDelayMs { get; set; } = 1000;
    public int MaxErrorDelayMs { get; set; } = 15000;
    public int BacklogRefreshIntervalSeconds { get; set; } = 15;
    public int MaxConsecutiveBatches { get; set; } = 200;
}
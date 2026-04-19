namespace CoEdit.Common.Infrastructure.Outbox;

public sealed class OutboxCleanupOptions
{
    public const string SectionName = "OutboxCleanup";

    public int RetentionDays { get; set; } = 7;
    public int CleanupIntervalMinutes { get; set; } = 60;
    public int DeleteBatchSize { get; set; } = 1000;
    public int PoisonScanIntervalMinutes { get; set; } = 5;
    public int MaxRetryCount { get; set; } = 3;
}
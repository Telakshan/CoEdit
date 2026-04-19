using System.Diagnostics.CodeAnalysis;

namespace CoEdit.Common.Infrastructure.Outbox;

[ExcludeFromCodeCoverage(Justification = "EF Core entity with no logic; persistence tested through integration tests")]
public class OutboxDeadLetter
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AssemblyQualifiedName { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public string SourceSchema { get; set; } = string.Empty;
    public DateTime QuarantinedAt { get; set; }
}
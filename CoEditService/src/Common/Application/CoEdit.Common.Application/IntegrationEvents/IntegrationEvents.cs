using System.Diagnostics.CodeAnalysis;
using CoEdit.Common.Application.Abstractions;

namespace CoEdit.Common.Application.IntegrationEvents;

public record UserRegisteredIntegrationEvent(Guid UserId, string Email, string FirstName, string LastName) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record UserDeactivatedIntegrationEvent(Guid UserId) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record DocumentCreatedIntegrationEvent(Guid DocumentId, string Title, Guid CreatorId) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record DocumentDeletedIntegrationEvent(Guid DocumentId, Guid DeletedBy) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record DocumentTitleUpdatedIntegrationEvent(
    Guid SourceEventId,
    Guid DocumentId,
    string Title,
    Guid UpdatedBy,
    DateTime OccurredAt) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record DocumentContentUpdatedIntegrationEvent(
    Guid SourceEventId,
    Guid DocumentId,
    string Delta,
    Guid UpdatedBy,
    DateTime OccurredAt) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record DocumentRestoredIntegrationEvent(
    Guid SourceEventId,
    Guid DocumentId,
    Guid RestoredBy,
    DateTime OccurredAt) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

[ExcludeFromCodeCoverage(Justification = "Integration event record with no logic; not yet consumed by any handler")]
public record DocumentSharedIntegrationEvent(Guid DocumentId, Guid UserId, string PermissionLevel) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

[ExcludeFromCodeCoverage(Justification = "Integration event record with no logic; not yet consumed by any handler")]
public record PermissionGrantedIntegrationEvent(Guid DocumentId, Guid UserId, string Role) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

[ExcludeFromCodeCoverage(Justification = "Integration event record with no logic; implicitly tested through handler tests")]
public record PermissionRevokedIntegrationEvent(Guid DocumentId, Guid UserId) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record UserJoinedDocumentIntegrationEvent(Guid UserId, Guid DocumentId, string ConnectionId) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record UserLeftDocumentIntegrationEvent(Guid UserId, Guid DocumentId, string ConnectionId) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record DocumentSavedIntegrationEvent(Guid DocumentId, Guid UserId, string Content, string Source = "unknown") : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record DocumentVersionRestoredIntegrationEvent(Guid DocumentId, Guid UserId, string Content) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
using CoEdit.Common.Application.Abstractions;

namespace CoEdit.Common.Application.IntegrationEvents;

public record UserRegisteredIntegrationEvent(Guid UserId, string Email, string FirstName, string LastName)
    : IIntegrationEvent
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

public record DocumentDeletedIntegrationEvent(Guid DocumentId) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record DocumentSharedIntegrationEvent(Guid DocumentId, Guid UserId, string PermissionLevel) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record PermissionGrantedIntegrationEvent(Guid DocumentId, Guid UserId, string Role) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

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
using CoEdit.Shared.Kernel.Abstractions;

namespace User.Domain.Events;

public record UserRegisteredDomainEvent(Guid UserId, string Email, Guid EventId, DateTime OccuredOnUtc): IDomainEvent
{
    public Guid EventId { get; } = EventId;
    public DateTime OccuredOnUtc { get; } = OccuredOnUtc;
}

public record EmailVerificationDomainEvent(Guid UserId, Guid EventId, DateTime OccuredOnUtc): IDomainEvent
{
    public Guid EventId { get; } = EventId;
    public DateTime OccuredOnUtc { get; } = OccuredOnUtc;
}

public record PasswordChangedDomainEvent(Guid UserId, Guid EventId, DateTime OccuredOnUtc): IDomainEvent
{
    public Guid EventId { get; } = EventId;
    public DateTime OccuredOnUtc { get; } = OccuredOnUtc;
}

public record UserDeactivatedDomainEvent(Guid UserId, Guid EventId, DateTime OccuredOnUtc): IDomainEvent
{
    public Guid EventId { get; } = EventId;
    public DateTime OccuredOnUtc { get; } = OccuredOnUtc;
}

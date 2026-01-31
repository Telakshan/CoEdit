using CoEdit.Common.Domain.Abstractions;

namespace User.Domain.Events;

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public record UserRegisteredDomainEvent(Guid UserId, string Email) : DomainEvent;
public record EmailVerifiedDomainEvent(Guid UserId) : DomainEvent;
public record PasswordChangedDomainEvent(Guid UserId) : DomainEvent;
public record UserDeactivatedDomainEvent(Guid UserId) : DomainEvent;
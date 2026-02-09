using CoEdit.Common.Domain.Abstractions;

namespace User.Domain.Events;

public class UserRegisteredDomainEvent(Guid userId, string email) : DomainEvent
{
    public Guid UserId { get; init; } = userId;
    public string Email { get; init; } = email;
}

public class EmailVerifiedDomainEvent(Guid userId) : DomainEvent
{
    public Guid UserId { get; init; } = userId;
}

public class PasswordChangedDomainEvent(Guid userId) : DomainEvent
{
    public Guid UserId { get; init; } = userId;
}

public class UserDeactivatedDomainEvent(Guid userId) : DomainEvent
{
    public Guid UserId { get; init; } = userId;
}
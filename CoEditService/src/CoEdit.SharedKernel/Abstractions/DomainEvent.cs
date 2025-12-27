namespace CoEdit.Shared.Kernel.Abstractions;

public interface DomainEvent : IDomainEvent
{
    Guid Id { get; }
    DateTime OccuredOnUtc { get; }
}
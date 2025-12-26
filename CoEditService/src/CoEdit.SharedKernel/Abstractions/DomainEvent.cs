namespace CoEdit.SharedKernel.Abstractions;

public interface DomainEvent : IDomainEvent
{
    Guid Id { get; }
    DateTime OccuredOnUtc { get; }
}
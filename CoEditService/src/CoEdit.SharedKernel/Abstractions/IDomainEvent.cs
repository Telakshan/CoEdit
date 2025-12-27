namespace CoEdit.Shared.Kernel.Abstractions;

public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccuredOnUtc { get; }
}
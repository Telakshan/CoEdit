namespace CoEdit.SharedKernel;

public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccuredOnUtc { get; }
}
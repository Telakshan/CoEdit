namespace CoEdit.Shared.Kernel.Common;

public interface IDomainEvent
{
     Guid Id { get; }
     DateTime OccuredOnUtc { get; }
}
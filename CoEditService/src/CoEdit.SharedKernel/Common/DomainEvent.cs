namespace CoEdit.Shared.Kernel.Common;

public class DomainEvent: IDomainEvent
{
    protected DomainEvent()
    {
        Id = Guid.NewGuid();
        OccuredOnUtc = DateTime.UtcNow;
    }

    protected DomainEvent(Guid id, DateTime occuredOnUtc)
    {
        Id = id;
        OccuredOnUtc = occuredOnUtc;
    }
    
    public Guid Id { get; init; }
    public DateTime OccuredOnUtc { get; init; }
}
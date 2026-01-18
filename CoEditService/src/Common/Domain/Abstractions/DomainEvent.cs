namespace CoEdit.Shared.Kernel.Abstractions;

public class DomainEvent: IDomainEvent
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccuredOnUtc = DateTime.UtcNow;
    }

    protected DomainEvent(Guid id, DateTime occuredOnUtc)
    {
        EventId = id;
        OccuredOnUtc = occuredOnUtc;
    }
    
    public Guid EventId { get; }
    public DateTime OccuredOnUtc { get; init; }
}
using CoEdit.Shared.Kernel.Abstractions;

namespace CoEdit.Shared.Kernel.Common;

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
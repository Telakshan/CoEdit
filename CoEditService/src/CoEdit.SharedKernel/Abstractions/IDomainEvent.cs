using MediatR;

namespace CoEdit.Shared.Kernel.Abstractions;

public interface IDomainEvent: INotification
{
     Guid EventId { get; }
     DateTime OccuredOnUtc { get; }
}
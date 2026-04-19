using MediatR;

namespace CoEdit.Common.Domain.Abstractions;

public interface IDomainEvent : INotification
{
     Guid EventId { get; }
     DateTime OccurredOn { get; }
}

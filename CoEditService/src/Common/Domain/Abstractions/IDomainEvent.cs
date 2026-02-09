using MediatR;

namespace CoEdit.Common.Domain.Abstractions;

public interface IDomainEvent: INotification
{
     Guid Id { get; }
     DateTime OccuredOnUtc { get; }
}
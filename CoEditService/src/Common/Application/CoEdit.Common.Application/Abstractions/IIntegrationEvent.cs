namespace CoEdit.Common.Application.Abstractions;

public interface IIntegrationEvent : INotification
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
}
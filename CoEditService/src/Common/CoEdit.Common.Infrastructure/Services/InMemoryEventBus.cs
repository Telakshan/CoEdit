using MediatR;

namespace CoEdit.Common.Infrastructure.Services;

public class InMemoryEventBus(IPublisher publisher) : IEventBus
{
    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : INotification
    {
        return publisher.Publish(@event, cancellationToken);
    }
}
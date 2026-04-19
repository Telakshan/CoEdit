using CoEdit.Common.Application.Abstractions;

namespace CoEdit.Common.Application.IntegrationEvents;

public interface IIntegrationEventPublisher
{
    void Publish(IIntegrationEvent integrationEvent);
}

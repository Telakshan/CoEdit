using CoEdit.Common.Domain.Abstractions;

namespace Collaboration.Domain.Documents;

public sealed class DocumentSavedIntegrationEvent(Guid documentId, string content)
    : DomainEvent
{
    public Guid DocumentId { get; init; } = documentId;
    public string Content { get; init; } = content;
}

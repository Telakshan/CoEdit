namespace Collaboration.Domain.Abstract;

public interface IDocumentPersistenceQueue
{
    void Enqueue(Guid documentId, Guid userId, string content, string source = "unknown");
    Task FlushDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
}
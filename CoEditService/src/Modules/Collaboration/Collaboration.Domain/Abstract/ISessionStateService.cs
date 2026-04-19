using Collaboration.Domain.Entities;

namespace Collaboration.Domain.Abstract;

public interface ISessionStateService
{
    Task AddSessionAsync(EditSession session);
    Task RemoveSessionAsync(Guid documentId, string connectionId);
    Task<IReadOnlyList<Guid>> RemoveSessionFromAllDocumentsAsync(string connectionId);
    Task UpdateCursorAsync(Guid documentId, string connectionId, object cursorData);
    Task<long> GetSessionCountAsync(Guid documentId);
    Task<List<string>> GetSessionsAsync(Guid documentId);
    Task<long> IncrementVersionAsync(Guid documentId);
    Task<long> GetVersionAsync(Guid documentId);
    Task AddOperationAsync(Operation op);
    Task<Operation?> GetOperationByIdAsync(Guid documentId, Guid operationId);
    Task<List<Operation>> GetOperationsAfterVersionAsync(Guid documentId, long version);
    Task<List<Operation>> GetOperationsAsync(Guid documentId);
    Task<List<Operation>> GetOperationsPaginatedAsync(Guid documentId, int skip, int take);
    Task SetDocumentContentAsync(Guid documentId, string content);
    Task<string?> GetDocumentContentAsync(Guid documentId);
    /// <summary>
    /// Atomically commits compacted state. Returns false if the document version changed
    /// since it was read (concurrent operation was committed), in which case no mutations are applied.
    /// </summary>
    Task<bool> CommitCompactionAsync(Guid documentId, string newContent, long expectedVersion, int retainedOps);
    Task ClearDocumentStateAsync(Guid documentId);
}
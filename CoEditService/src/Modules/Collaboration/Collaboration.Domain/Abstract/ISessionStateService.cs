using Collaboration.Domain.Entities;

namespace Collaboration.Domain.Abstract;

public interface ISessionStateService
{
    Task AddSessionAsync(EditSession session);
    Task RemoveSessionAsync(Guid documentId, string connectionId);
    Task UpdateCursorAsync(Guid documentId, string connectionId, object cursorData);
    Task<List<string>> GetSessionsAsync(Guid documentId);
    Task<long> IncrementVersionAsync(Guid documentId);
    Task<long> GetVersionAsync(Guid documentId);
    Task AddOperationAsync(Operation op);
    Task<List<Operation>> GetOperationsAfterVersionAsync(Guid documentId, long version);
}
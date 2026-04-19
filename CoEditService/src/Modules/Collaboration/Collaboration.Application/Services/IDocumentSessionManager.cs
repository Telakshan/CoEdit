using Collaboration.Domain.Entities;

namespace Collaboration.Application.Services;

public interface IDocumentSessionManager
{
    Task<EditSession> JoinDocumentAsync(Guid documentId, Guid userId, string connectionId, string displayName);
    Task LeaveDocumentAsync(Guid documentId, Guid userId, string connectionId);
    Task<IEnumerable<EditSession>> GetActiveSessionsAsync(Guid documentId);
    Task UpdateActivityAsync(Guid documentId, Guid userId);
    Task<bool> IsUserInSessionAsync(Guid documentId, Guid userId);
}
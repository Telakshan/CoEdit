using Collaboration.Domain.Entities;

namespace Collaboration.Application.Services;

public interface IOperationManager
{
    Task<long> GetNextVersionAsync(Guid documentId);
    Task<long> GetCurrentVersionAsync(Guid documentId);
    Task QueueOperationAsync(Guid documentId, Operation operation);
    Task<IEnumerable<Operation>> GetPendingOperationsAsync(Guid documentId, long afterVersion);
    Task<Operation> ProcessOperationAsync(Guid documentId, Operation operation, long clientBaseVersion);
}
using CoEdit.Common.Domain.Abstractions;

namespace User.Domain.Repositories;

public interface IUserRepository : IRepository<Entities.User>
{
    Task<Entities.User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default); // Ensure base IRepository has this or add here
    Task<Entities.User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default); // Added missing method
    Task AddAsync(Entities.User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(Entities.User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(Entities.User user, CancellationToken cancellationToken = default);
}
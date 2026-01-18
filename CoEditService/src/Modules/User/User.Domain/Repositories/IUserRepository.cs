using CoEdit.Shared.Kernel.Abstractions;

namespace User.Domain.Repositories;

public interface IUserRepository: IAsyncRepository<Entities.User, Guid>
{
    Task<Entities.User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default);
}
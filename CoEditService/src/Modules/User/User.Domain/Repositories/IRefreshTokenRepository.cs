using CoEdit.Shared.Kernel.Abstractions;
using User.Domain.Entities;

namespace User.Domain.Repositories;

public interface IRefreshTokenRepository: IAsyncRepository<RefreshToken, Guid>
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
using CoEdit.Common.Domain.Abstractions;
using User.Domain.Entities;

namespace User.Domain.Repositories;

public interface IRefreshTokenRepository: IRepository<RefreshToken>
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default); 
}
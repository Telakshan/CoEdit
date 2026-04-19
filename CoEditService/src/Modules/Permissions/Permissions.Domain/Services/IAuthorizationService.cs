namespace Permissions.Domain.Services;

public interface IAuthorizationService
{
    Task<bool> CanAccessAsync(Guid userId, Guid resourceId, AccessLevel requiredLevel);
    Task<AccessLevel?> GetAccessLevelAsync(Guid userId, Guid resourceId);
    Task InvalidatePermissionCacheAsync(Guid userId, Guid resourceId);
}

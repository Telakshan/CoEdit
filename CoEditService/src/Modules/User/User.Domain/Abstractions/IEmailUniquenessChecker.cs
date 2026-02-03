namespace User.Domain.Abstractions;

public interface IEmailUniquenessChecker
{
    Task<bool> IsUniqueAsync(string email, CancellationToken cancellationToken = default);
}
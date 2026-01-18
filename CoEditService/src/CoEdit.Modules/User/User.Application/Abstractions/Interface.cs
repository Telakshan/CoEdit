namespace User.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles);
    string GenerateRefreshToken();
    DateTime GetAccessTokenExpirationDate();
    DateTime GetRefreshTokenExpirationDate();
}

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}
using CoEdit.Common.Domain.Abstractions;

namespace User.Domain.Entities;

public class RefreshToken: AggregateRoot
{
    public string Token { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }

    private RefreshToken()
    {
    }

    private RefreshToken(string token, Guid userId, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        Token = token;
        UserId = userId;
        ExpiresAt = expiresAt;
        IsRevoked = false;
    }

    public static RefreshToken Create(string token, Guid userId, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be empty");
        }

        return userId == Guid.Empty ? throw new ArgumentException("User ID cannot be empty") : new RefreshToken(token, userId, expiresAt);
    }
    
    public void Revoke()
    {
        if (IsRevoked) return;
        IsRevoked = true;
    }

    public bool IsValid()
    {
        return !IsRevoked && DateTime.UtcNow < ExpiresAt;
    }
}
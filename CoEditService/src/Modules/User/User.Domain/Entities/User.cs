using CoEdit.Common.Domain.Abstractions;
using User.Domain.Abstractions;
using User.Domain.Events;
using User.Domain.Exceptions;
using User.Domain.ValueObjects;

namespace User.Domain.Entities;

public class User: AggregateRoot
{
    public Email Email { get; private set; }
    public Password PasswordHash { get; private set; }
    public SecurityStamp SecurityStamp { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public UserProfile Profile { get; private set; }

    // Constructor for EF Core
    private User() { }

    private User(Email email, Password password, string verificationToken, DateTime createdAt)
    {
        Id = Guid.NewGuid();
        Email = email;
        PasswordHash = password;
        SecurityStamp = SecurityStamp.Create();
        IsEmailVerified = false;
        EmailVerificationToken = verificationToken;
        EmailVerificationTokenExpiresAt = createdAt.AddDays(1); // Default expiration
        IsActive = true;
        CreatedAt = createdAt;

        // Default profile
        Profile = UserProfile.Create(Id, email.Value.Split('@')[0]);

        AddDomainEvent(new UserRegisteredDomainEvent(Id, Email.Value));
    }

    public static User Register(string email, string password, IPasswordHasher passwordHasher)
    {
        var emailVo = Email.Create(email);
        var passwordVo = Password.Create(password, passwordHasher);
        // Generate a simple token for verification. In production, consider a more robust generation.
        var token = Guid.NewGuid().ToString("N");
        return new User(emailVo, passwordVo, token, DateTime.UtcNow);
    }

    public void VerifyEmail(string token)
    {
        if (IsEmailVerified) return;

        if (EmailVerificationToken != token)
        {
            throw new UserDomainException("Invalid verification token.");
        }

        if (DateTime.UtcNow > EmailVerificationTokenExpiresAt)
        {
            throw new UserDomainException("Verification token has expired.");
        }

        IsEmailVerified = true;
        EmailVerificationToken = null;
        EmailVerificationTokenExpiresAt = null;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new EmailVerifiedDomainEvent(Id));
    }

    public void ChangePassword(string currentPassword, string newPassword, IPasswordHasher passwordHasher)
    {
        if (!IsEmailVerified)
        {
            throw new UserDomainException("Cannot change password without email verification.");
        }

        if (!PasswordHash.Verify(currentPassword, passwordHasher))
        {
            throw new UserDomainException("Invalid current password.");
        }

        PasswordHash = Password.Create(newPassword, passwordHasher);
        SecurityStamp = SecurityStamp.Create(); // Rotate security stamp
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PasswordChangedDomainEvent(Id));
    }

    public void UpdateProfile(string displayName, string? avatarUrl, string? timeZone, string? preferences)
    {
        Profile.Update(displayName, avatarUrl, timeZone, preferences);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new UserDeactivatedDomainEvent(Id));
    }

    public void Reactivate()
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
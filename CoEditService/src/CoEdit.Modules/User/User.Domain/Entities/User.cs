using System.ComponentModel.DataAnnotations;
using CoEdit.Shared.Kernel.Common;
using User.Domain.Abstractions;
using User.Domain.Events;
using User.Domain.Exceptions;
using User.Domain.ValueObjects;

namespace User.Domain.Entities;

public class User: Entity<Guid>
{
    public Email Email { get; private set; }
    private Password PasswordHash { get; set; }
    public SecurityStamp SecurityStamp { get; private set; }
    private bool IsEmailVerified { get; set; }
    private string? EmailVerificationToken { get; set; }
    private DateTime? EmailVerificationTokenExpiresAt { get; set; }
    private bool IsActive { get; set; }

    private UserProfile Profile { get; set;  }
    
    private User(){}

    private User(Email email, Password password, string verificationToken, DateTime createdAt)
    {
        Email = email;
        PasswordHash = password;
        SecurityStamp = SecurityStamp.Create();
        IsEmailVerified = false;
        EmailVerificationToken = verificationToken;
        EmailVerificationTokenExpiresAt = createdAt.AddDays(1);
        IsActive = true;
        CreatedAt = createdAt;

        Profile = UserProfile.Create(Id, email.Value.Split('@')[0]);

        Raise(new UserRegisteredDomainEvent(Id, email.Value, Guid.NewGuid(), CreatedAt));
    }

    public static User Register(string email, string password, IPasswordHasher passwordHasher)
    {
        var emailValueObject = Email.Create(email);
        var passwordValueObject = Password.Create(password, passwordHasher);
        var token = Guid.NewGuid().ToString("N");
        return new User(emailValueObject, passwordValueObject, token, DateTime.UtcNow);
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
        Raise(new EmailVerificationDomainEvent(Id, Guid.NewGuid(), UpdatedAt));
    }

    public void ChangePassword(string  currentPassword, string newPassword, IPasswordHasher passwordHasher)
    {
        if (!IsEmailVerified)
        {
            throw new UserDomainException("Cannot change password for unverified email");
        }

        if (!PasswordHash.Verify(currentPassword, passwordHasher))
        {
            throw new UserDomainException("Invalid current password");
        }

        PasswordHash = Password.Create(newPassword, passwordHasher);
        SecurityStamp = SecurityStamp.Create();
        UpdatedAt = DateTime.UtcNow;
        Raise(new PasswordChangedDomainEvent(Id, Guid.NewGuid(), UpdatedAt));
    }

    public void UpdateProfile(string displayName, string? avatarUrl, string? timeZone, string? preferences)
    {
        if (timeZone != null) Profile.Update(displayName, avatarUrl, timeZone, preferences);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if(!IsActive) return;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        Raise(new UserDeactivatedDomainEvent(Id, Guid.NewGuid(), UpdatedAt));
    }

    public void Reactivate()
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
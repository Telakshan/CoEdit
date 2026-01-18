using CoEdit.Shared.Kernel;
using CoEdit.Shared.Kernel.Common;
using User.Domain.ValueObjects;

namespace User.Domain.Entities;

public class User: Entity<Guid>
{
    public Email Email { get; private set; }
    public Password PasswordHash { get; private set; }
    public SecurityStamp SecurityStamp { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; private set; }
    public bool IsActive { get; private set; }
    
    public UserProfile Profile { get; private set;  }
    
    private User(){}

    private User(Email email, Password password, string verificationToken, DateTime createdAt)
    {
        Email = email;
        PasswordHash = password;
        EmailVerificationToken = verificationToken;
        EmailVerificationTokenExpiresAt = createdAt.AddDays(1);
        IsActive = true;
        CreatedAt = createdAt;

        Profile = UserProfile.Create(Id, email.Value.Split('@')[0]);
        
        AddDomainEvent(new UserRegisteredDomainEvent(Id, Email.Value))
    }
}
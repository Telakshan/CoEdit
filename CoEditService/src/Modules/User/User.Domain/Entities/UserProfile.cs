using CoEdit.Common.Domain.Abstractions;

namespace User.Domain.Entities;

public class UserProfile: Entity
{
    public string DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? TimeZone { get; private set; }
    public string? Preferences { get; private set; }
    
    private UserProfile(){}
    
    internal UserProfile(Guid id, string displayName, string? avatarUrl, string timeZone, string? preferences)
    {
        Id = id;
        DisplayName = displayName;
        
        AvatarUrl = avatarUrl;
        TimeZone = timeZone;
        Preferences = preferences;
    }

    public static UserProfile Create(Guid id, string displayName, string? avatarUrl = null)
    {
        return string.IsNullOrWhiteSpace(displayName) ? throw new ArgumentException("Display name cannot be empty") : new UserProfile(id, displayName, avatarUrl, null, null);
    }

    public void Update(string displayName, string? avatarUrl, string timeZone, string? preferences)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name cannot be empty.");
        }
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        TimeZone = timeZone;
        Preferences = preferences;
    }
}
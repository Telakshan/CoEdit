namespace Collaboration.Domain.Entities;

public class EditSession
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; private set; }
    public Guid UserId { get; private set; }
    public string ConnectionId { get; private set; }
    public string DisplayName { get; set; } = string.Empty;
    public string UserColor { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public DateTime LastActivity { get; set; }

    public EditSession(Guid documentId, Guid userId, string connectionId)
    {
        DocumentId = documentId;
        UserId = userId;
        ConnectionId = connectionId;
        JoinedAt = DateTime.UtcNow;
        LastActivity = DateTime.UtcNow;
    }

    // Empty constructor for serialization
    public EditSession() { }

    public void UpdateActivity()
    {
        LastActivity = DateTime.UtcNow;
    }

    public bool IsActive(TimeSpan timeout)
    {
        return (DateTime.UtcNow - LastActivity) < timeout;
    }
}
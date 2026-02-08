namespace Collaboration.Core.ValueObjects;

public class UserPresence
{
    public Guid UserId { get; set; }
    public Guid DocumentId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsTyping { get; set; }
    public CursorPosition? CursorPosition { get; set; }
    public DateTime LastSeen { get; set; }
}
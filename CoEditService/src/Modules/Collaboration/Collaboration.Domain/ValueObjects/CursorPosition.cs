namespace Collaboration.Domain.ValueObjects;

public class CursorPosition
{
    public Guid UserId { get; set; }
    public Guid DocumentId { get; set; }
    public int Position { get; set; }
    public int? SelectionStart { get; set; }
    public int? SelectionEnd { get; set; }
    public DateTime LastUpdated { get; set; }
}
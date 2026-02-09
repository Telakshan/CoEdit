namespace Collaboration.Domain.Entities;

public class Operation
{
    public Guid OperationId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public OperationType Type { get; set; }
    public int Position { get; set; }
    public string? Content { get; set; } 
    public int Length { get; set; } 
    public long Timestamp { get; set; }
    public int Version { get; set; }

    public Operation(Guid documentId, Guid userId, OperationType type, int position, string? content, int version)
    {
        OperationId = Guid.NewGuid();
        DocumentId = documentId;
        UserId = userId;
        Type = type;
        Position = position;
        Content = content;
        Version = version;
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public Operation() { }
}

public enum OperationType
{
    Insert,
    Delete,
    Retain
}

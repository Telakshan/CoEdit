using System.Text.Json.Serialization;

namespace Collaboration.Domain.Entities;

public enum OperationType
{
    Insert,
    Delete,
    Retain
}

public class Operation
{
    public Guid OperationId { get; init; }
    public Guid DocumentId { get; init; }
    public Guid UserId { get; init; }
    public long Timestamp { get; init; }

    [JsonInclude] public OperationType Type { get; internal set; }
    [JsonInclude] public int Position { get; internal set; }
    [JsonInclude] public string? Content { get; internal set; }
    [JsonInclude] public int Length { get; internal set; }
    [JsonInclude] public int Version { get; internal set; }

    public Operation(Guid documentId, Guid userId, OperationType type, int position, string? content, int version, int length = 0)
    {
        OperationId = Guid.NewGuid();
        DocumentId = documentId;
        UserId = userId;
        Type = type;
        Position = position;
        Content = content;
        Version = version;
        Length = length;
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    // Empty constructor for serialization
    [JsonConstructor]
    public Operation() { }

    /// <summary>
    /// Sets the server-assigned version after OT processing.
    /// </summary>
    public void SetVersion(int version) => Version = version;
}

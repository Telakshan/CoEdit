
using Collaboration.Domain.Entities;

namespace Collaboration.Application.DataTransferObjects;

public class OperationDto
{
    public Guid DocumentId { get; init; }
    public Guid UserId { get; init; }
    public string Type { get; init; } = string.Empty; // "Insert", "Delete", "Retain"
    public int Position { get; init; }
    public string? Content { get; init; }
    public int Version { get; init; }
    public Guid? ClientOperationId { get; init; } // For acknowledgment

    public Operation ToEntity()
    {
        Enum.TryParse<OperationType>(Type, true, out var opType);
        return new Operation(DocumentId, UserId, opType, Position, Content, Version);
    }
}

using System.Diagnostics.CodeAnalysis;
using CoEdit.Common.Domain.Shared;
using Collaboration.Domain.Entities;

namespace Collaboration.Application.DataTransferObjects;

[ExcludeFromCodeCoverage(Justification = "DTO model with minimal mapping logic; behavior covered via command handler tests")]
public sealed record OperationDto
{
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public Guid ClientOperationId { get; set; } // Required for idempotent client acknowledgments
    public string Type { get; set; } = string.Empty; // "Insert", "Delete", "Retain"
    public int Position { get; set; }
    public int Length { get; set; } // Required for Delete/Retain
    public string? Content { get; set; }
    public int BaseVersion { get; set; } // Client-side document version this operation was authored against
    public int Version { get; set; } // Server-assigned version after processing

    public Result<Operation> ToEntity()
    {
        return !Enum.TryParse<OperationType>(Type, true, out var opType) ? Result.Failure<Operation>($"Unsupported operation type '{Type}'.") : Result.Success(new Operation(DocumentId, UserId, opType, Position, Content, BaseVersion, Length));
    }
}
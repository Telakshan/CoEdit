using Collaboration.Domain.Entities;

namespace CoEdit.Common.Application.Dto;

public class OperationDto
{
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Position { get; set; }
    public string? Content { get; set; }
    public int Version { get; set; }
    public Guid? ClientOperationId { get; set; } 

    public Operation ToEntity()
    {
        Enum.TryParse<OperationType>(Type, true, out var opType);
        return new Operation(DocumentId, UserId, opType, Position, Content, Version);
    }
}
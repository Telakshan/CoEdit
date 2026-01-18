namespace CoEdit.Modules.History;
public interface IHistoryService
{
    Task SaveDeltasAsync(IEnumerable<DocumentDeltaDto> deltas);
}

public record DocumentDeltaDto(Guid DocumentId, string Delta, DateTime Timestamp);
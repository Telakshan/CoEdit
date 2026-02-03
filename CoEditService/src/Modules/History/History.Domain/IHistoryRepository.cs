namespace CoEdit.Modules.History;

public interface IHistoryRepository
{
    Task SaveHistoryAsync(string documentId, List<string> history);
}

public class DocumentEntity
{
    public Guid DocumentId { get; set; } =  Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}
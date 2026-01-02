namespace CoEdit.Modules.History;
public interface IHistoryService
{
    Task SaveHistoryAsync(string documentId, List<string> history);
}

public class DocumentEntity
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string LastUpdated { get; set;}
}
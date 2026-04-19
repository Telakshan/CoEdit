namespace Collaboration.Infrastructure.ValueObjects;

public static class RedisKeyConstants
{
    private const string DocPrefix = "doc";
    private const string ConnPrefix = "conn";

    public static string DocumentVersion(Guid documentId) => $"{DocPrefix}:{documentId}:version";
    public static string DocumentContent(Guid documentId) => $"{DocPrefix}:{documentId}:content";
    public static string DocumentOperations(Guid documentId) => $"{DocPrefix}:{documentId}:ops";
    public static string DocumentOpById(Guid documentId) => $"{DocPrefix}:{documentId}:op-by-id";
    public static string DocumentOpIdIndex(Guid documentId) => $"{DocPrefix}:{documentId}:op-id-index";
    public static string DocumentSessions(Guid documentId) => $"{DocPrefix}:{documentId}:sessions";
    public static string DocumentCursors(Guid documentId) => $"{DocPrefix}:{documentId}:cursors";
    public static string DocumentPresence(Guid documentId) => $"{DocPrefix}:{documentId}:presence";
    public static string DocumentTyping(Guid documentId) => $"{DocPrefix}:{documentId}:typing";
    
    public static string ConnectionDocuments(string connectionId) => $"{ConnPrefix}:{connectionId}:docs";
    public static string Lock(string key) => $"lock:{key}";
}
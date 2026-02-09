namespace Collaboration.Application.DataTransferObjects;

public class UserJoinedDto
{
    public Guid UserId { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class UserLeftDto
{
    public Guid UserId { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
}

public class DocumentStateDto
{
    public IEnumerable<UserPresence> ActiveUsers { get; set; } = new List<UserPresence>();
    public long CurrentVersion { get; set; }
}

public class UserPresence
{
    public Guid UserId { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
}
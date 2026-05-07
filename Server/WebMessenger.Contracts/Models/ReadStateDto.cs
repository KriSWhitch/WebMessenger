namespace WebMessenger.Contracts.Models;

public sealed record ReadStateDto
{
    public Guid ChatId { get; init; }
    public Guid UserId { get; init; }
    public DateTime LastReadAt { get; init; }
    public int UnreadCount { get; init; }
}

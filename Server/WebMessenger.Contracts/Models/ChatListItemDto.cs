namespace WebMessenger.Contracts.Models;

public sealed record ChatListItemDto
{
    public Guid Id { get; init; }
    public bool IsGroup { get; init; }
    public string? Title { get; init; }
    public string? AvatarUrl { get; init; }
    public DateTime LastActivityAt { get; init; }
    public ChatMessagePreviewDto? LastMessage { get; init; }
    public int UnreadCount { get; init; }
    public bool HasUnread => UnreadCount > 0;
    public Guid? PeerUserId { get; init; }
    public string? PeerUsername { get; init; }
    public string? PeerAvatarUrl { get; init; }
}
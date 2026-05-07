namespace WebMessenger.Contracts.Models;

public sealed record DirectChatHeaderDto
{
    public Guid OtherUserId { get; init; }
    public string? Username { get; init; }
    public string? AvatarUrl { get; init; }
    public bool IsOnline { get; init; }
    public Guid? ChatId { get; init; }
}

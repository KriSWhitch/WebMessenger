namespace WebMessenger.Contracts.Models;

public sealed record ChatMessageDto
{
    public Guid Id { get; init; }
    public Guid ChatId { get; init; }
    public Guid SenderId { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime SentAt { get; init; }
    public DateTime? EditedAt { get; init; }
}

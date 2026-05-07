namespace WebMessenger.Contracts.Models;

public sealed record ChatMessagePreviewDto
{
    public Guid Id { get; init; }
    public Guid SenderId { get; init; }
    public string Snippet { get; init; } = string.Empty;
    public DateTime SentAt { get; init; }
}

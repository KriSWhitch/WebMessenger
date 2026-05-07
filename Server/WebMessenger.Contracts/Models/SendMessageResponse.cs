namespace WebMessenger.Contracts.Models;

public sealed record SendMessageResponse(Guid ChatId, ChatMessageDto Message);

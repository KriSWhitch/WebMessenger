namespace WebMessenger.Contracts.Models
{
    public sealed record MessageCreatedPayload(Guid ChatId, Guid? PeerUserId, ChatMessageDto Message);
    public sealed record ReadReceiptPayload(Guid ChatId, Guid UserId, DateTime LastReadAt);
    public sealed record TypingPayload(Guid ChatId, Guid UserId, bool IsTyping);
}

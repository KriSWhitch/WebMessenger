using WebMessenger.Api.Models;

namespace WebMessenger.Api.Hubs.Events.Interfaces
{
    public interface IChatEvents
    {
        Task MessageCreatedAsync(Guid chatId, ChatMessageDto message, Guid? peerUserId = null, CancellationToken ct = default);
        Task ReadReceiptAsync(Guid chatId, Guid userId, DateTime lastReadAtUtc, CancellationToken ct = default);
        Task TypingAsync(Guid chatId, Guid userId, bool isTyping, CancellationToken ct = default);

    }
}

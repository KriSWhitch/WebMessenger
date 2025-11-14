using WebMessenger.Api.Models;

namespace WebMessenger.Api.Services.Interfaces
{
    public interface IChatService
    {
        Task<Guid?> GetDirectChatIdAsync(Guid currentUserId, Guid otherUserId);
        Task<DirectChatHeaderDto?> GetChatHeaderByChatIdAsync(Guid currentUserId, Guid chatId);
        Task<DirectChatHeaderDto> GetDirectChatHeaderAsync(Guid currentUserId, Guid otherUserId);
        Task<PagedResult<ChatListItemDto>> GetUserChatsAsync(Guid currentUserId, int limit, DateTime? before);
        Task<PagedResult<ChatMessageDto>> GetMessagesAsync(Guid currentUserId, Guid chatId, int limit, DateTime? before);
        Task<SendMessageResponse> SendMessageToUserAsync(Guid currentUserId, Guid otherUserId, string content);
        Task<ReadStateDto> MarkChatReadAsync(Guid currentUserId, Guid chatId, DateTime? atUtc = null);
        Task<ReadStateDto> GetReadStateAsync(Guid currentUserId, Guid chatId);
        Task<Guid?> TryGetDirectPeerAsync(Guid chatId, Guid me, CancellationToken ct = default);
    }
}

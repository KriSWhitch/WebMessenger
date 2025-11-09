using WebMessenger.Api.Models;

namespace WebMessenger.Api.Services.Interfaces
{
    public interface IChatService
    {
        Task<Guid?> GetDirectChatIdAsync(Guid currentUserId, Guid otherUserId);
        Task<DirectChatHeaderDto> GetDirectChatHeaderAsync(Guid currentUserId, Guid otherUserId);
        Task<PagedResult<ChatMessageDto>> GetMessagesAsync(Guid currentUserId, Guid chatId, int limit, DateTime? before);
        Task<SendMessageResponse> SendMessageToUserAsync(Guid currentUserId, Guid otherUserId, string content);
    }
}

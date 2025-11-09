using Microsoft.EntityFrameworkCore;
using WebMessenger.Api.Models;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.DAL.Entities;
using WebMessenger.DAL.Interfaces;

namespace WebMessenger.Api.Services
{
    public class ChatService(IUnitOfWork uow) : IChatService
    {
        private readonly IUnitOfWork _uow = uow;

        public async Task<Guid?> GetDirectChatIdAsync(Guid me, Guid other)
        {
            return await _uow.ChatRepository
                .GetAll(nameof(Chat.Members))
                .Where(c => !c.IsGroup)
                .Where(c =>
                    c.Members.Any(m => m.UserId == me) &&
                    c.Members.Any(m => m.UserId == other))
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<DirectChatHeaderDto> GetDirectChatHeaderAsync(Guid me, Guid other)
        {
            var otherUser = await _uow.UserRepository.GetAll()
                .Where(u => u.Id == other)
                .Select(u => new { u.Id, u.Username, u.AvatarUrl, u.IsOnline })
                .FirstOrDefaultAsync();

            if (otherUser == null)
                throw new InvalidOperationException("User not found");

            var chatId = await GetDirectChatIdAsync(me, other);

            return new DirectChatHeaderDto
            {
                OtherUserId = otherUser.Id,
                Username = otherUser.Username,
                AvatarUrl = otherUser.AvatarUrl,
                IsOnline = otherUser.IsOnline,
                ChatId = chatId
            };
        }

        public async Task<PagedResult<ChatMessageDto>> GetMessagesAsync(Guid me, Guid chatId, int limit, DateTime? before)
        {
            var isMember = await _uow.ChatMemberRepository.GetAll()
                .AnyAsync(cm => cm.ChatId == chatId && cm.UserId == me);
            if (!isMember) throw new UnauthorizedAccessException("Not a member of this chat");

            var q = _uow.MessageRepository.GetAll()
                .Where(m => m.ChatId == chatId);

            if (before.HasValue)
                q = q.Where(m => m.SentAt < before.Value);

            var take = Math.Clamp(limit, 1, 200);

            var items = await q
                .OrderByDescending(m => m.SentAt)
                .Take(take)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    ChatId = m.ChatId,
                    SenderId = m.SenderId,
                    Content = m.Content,
                    SentAt = m.SentAt,
                    EditedAt = m.EditedAt,
                    IsRead = m.IsRead
                })
                .ToListAsync();

            items.Reverse();

            return new PagedResult<ChatMessageDto>
            {
                Items = items,
                HasMore = items.Count == take,
                NextBefore = items.Count > 0 ? items.First().SentAt : before
            };
        }

        public async Task<SendMessageResponse> SendMessageToUserAsync(Guid me, Guid other, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content is required");

            var chatId = await GetDirectChatIdAsync(me, other);

            if (!chatId.HasValue)
            {
                var chat = new Chat { IsGroup = false };
                _uow.ChatRepository.Insert(chat);

                var cm1 = new ChatMember { Chat = chat, UserId = me };
                var cm2 = new ChatMember { Chat = chat, UserId = other };
                _uow.ChatMemberRepository.CreateRange(new[] { cm1, cm2 });

                await _uow.CommitAsync();
                chatId = chat.Id;
            }

            var message = new Message
            {
                ChatId = chatId.Value,
                SenderId = me,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            _uow.MessageRepository.Insert(message);
            await _uow.CommitAsync();

            return new SendMessageResponse
            {
                ChatId = chatId.Value,
                Message = new ChatMessageDto
                {
                    Id = message.Id,
                    ChatId = message.ChatId,
                    SenderId = message.SenderId,
                    Content = message.Content,
                    SentAt = message.SentAt,
                    EditedAt = message.EditedAt,
                    IsRead = message.IsRead
                }
            };
        }
    }
}

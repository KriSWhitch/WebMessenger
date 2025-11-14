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

        public async Task<PagedResult<ChatListItemDto>> GetUserChatsAsync(Guid me, int limit, DateTime? before)
        {
            var take = Math.Clamp(limit, 1, 200);

            var baseQuery =
                from c in _uow.ChatRepository.GetAll(nameof(Chat.Members))
                where c.Members.Any(m => m.UserId == me)
                select new
                {
                    Chat = c,
                    LastMessage = _uow.MessageRepository.GetAll()
                        .Where(m => m.ChatId == c.Id)
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => new { m.Id, m.SenderId, m.Content, m.SentAt })
                        .FirstOrDefault(),
                    MyLastReadAt = _uow.ChatMemberRepository.GetAll()
                        .Where(cm => cm.ChatId == c.Id && cm.UserId == me)
                        .Select(cm => cm.LastReadAt)
                        .FirstOrDefault(),
                    LastActivityAt = _uow.MessageRepository.GetAll()
                        .Where(m => m.ChatId == c.Id)
                        .Max(m => (DateTime?)m.SentAt) ?? c.CreatedAt
                };

            if (before.HasValue)
                baseQuery = baseQuery.Where(x => x.LastActivityAt < before.Value);

            var rows = await baseQuery
                .OrderByDescending(x => x.LastActivityAt)
                .Take(take)
                .ToListAsync();

            var chatIds = rows.Select(r => r.Chat.Id).ToArray();

            var lastReads = _uow.ChatMemberRepository.GetAll()
                .Where(cm => cm.UserId == me && chatIds.Contains(cm.ChatId))
                .Select(cm => new { cm.ChatId, cm.LastReadAt })
                .ToList();

            var unreadCounts = _uow.MessageRepository.GetAll()
                .Where(m => chatIds.Contains(m.ChatId) && m.SenderId != me)
                .GroupBy(m => m.ChatId)
                .Select(g => new { ChatId = g.Key, Count = g.Count() })
                .ToList();


            var items = rows.Select(r =>
            {
                var readAt = lastReads.FirstOrDefault(x => x.ChatId == r.Chat.Id)?.LastReadAt ?? DateTime.MinValue;
                var unread = _uow.MessageRepository.GetAll()
                  .Count(m => m.ChatId == r.Chat.Id && m.SenderId != me && m.SentAt > readAt);

                Guid? peerId = null;
                string? peerName = null;
                string? peerAvatar = null;

                if (!r.Chat.IsGroup)
                {
                    var memberIds = r.Chat.Members.Select(m => m.UserId).ToArray();
                    peerId = memberIds.FirstOrDefault(x => x != me);
                    if (peerId.HasValue)
                    {
                        var peer = _uow.UserRepository.GetAll().FirstOrDefault(u => u.Id == peerId.Value);
                        if (peer != null)
                        {
                            peerName = peer.Username;
                            peerAvatar = peer.AvatarUrl;
                        }
                    }
                }

                return new ChatListItemDto
                {
                    Id = r.Chat.Id,
                    IsGroup = r.Chat.IsGroup,
                    Title = r.Chat.IsGroup ? r.Chat.Name : (peerName ?? r.Chat.Name),
                    AvatarUrl = r.Chat.IsGroup ? r.Chat.AvatarUrl : (peerAvatar ?? r.Chat.AvatarUrl),
                    LastActivityAt = r.LastActivityAt,
                    LastMessage = r.LastMessage == null ? null : new ChatMessagePreviewDto
                    {
                        Id = r.LastMessage.Id,
                        SenderId = r.LastMessage.SenderId,
                        Snippet = r.LastMessage.Content.Length > 120 ? r.LastMessage.Content[..120] + "…" : r.LastMessage.Content,
                        SentAt = r.LastMessage.SentAt
                    },
                    UnreadCount = unread,
                    PeerUserId = peerId,
                    PeerUsername = peerName,
                    PeerAvatarUrl = peerAvatar
                };
            }).ToList();

            return new PagedResult<ChatListItemDto>
            {
                Items = items,
                HasMore = items.Count == take,
                NextBefore = items.Count > 0 ? items.Last().LastActivityAt : before
            };
        }

        public async Task<DirectChatHeaderDto?> GetChatHeaderByChatIdAsync(Guid me, Guid chatId)
        {
            var chat = await _uow.ChatRepository
                .GetAll(nameof(Chat.Members))
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null) return null;

            if (!chat.IsGroup)
            {
                var otherId = chat.Members
                    .Select(m => m.UserId)
                    .FirstOrDefault(uid => uid != me);

                if (otherId == Guid.Empty) return null;

                var other = await _uow.UserRepository
                    .GetAll()
                    .Select(u => new { u.Id, u.Username, u.AvatarUrl, u.IsOnline })
                    .FirstOrDefaultAsync(u => u.Id == otherId);

                if (other == null) return null;

                return new DirectChatHeaderDto
                {
                    OtherUserId = other.Id,
                    Username = other.Username,
                    AvatarUrl = other.AvatarUrl,
                    IsOnline = other.IsOnline,
                    ChatId = chat.Id
                };
            }

            return new DirectChatHeaderDto
            {
                OtherUserId = Guid.Empty,
                Username = chat.Name,
                AvatarUrl = chat.AvatarUrl,
                IsOnline = false,
                ChatId = chat.Id
            };
        }

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

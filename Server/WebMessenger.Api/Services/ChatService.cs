using Microsoft.EntityFrameworkCore;
using WebMessenger.Api.Projections.Messages;
using WebMessenger.Api.Projections.Users;
using WebMessenger.Contracts.Models;
using WebMessenger.DAL.Entities;
using WebMessenger.DAL.Interfaces;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Api.Hubs.Events.Interfaces;

namespace WebMessenger.Api.Services
{
    public class ChatService(IUnitOfWork uow, IChatEvents events, ILogger<ChatService> logger) : IChatService
    {
        private readonly IUnitOfWork _uow = uow;
        private readonly IChatEvents _events = events;
        private readonly ILogger<ChatService> _logger = logger;

        public async Task<PagedResult<ChatListItemDto>> GetUserChatsAsync(Guid me, int limit, DateTime? before)
        {
            var take = Math.Clamp(limit, 1, 200);
            _logger.LogDebug("Fetching chats for user {UserId}, limit={Limit}, before={Before}", me, take, before);

            try
            {
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

                // Batch-fetch last reads for all chats in one query
                var lastReadMap = await _uow.ChatMemberRepository.GetAll()
                    .Where(cm => cm.UserId == me && chatIds.Contains(cm.ChatId))
                    .Select(cm => new { cm.ChatId, cm.LastReadAt })
                    .ToDictionaryAsync(x => x.ChatId, x => x.LastReadAt);

                // Batch-fetch unread counts per chat in one query
                var minReadTimes = chatIds.ToDictionary(
                    id => id,
                    id => lastReadMap.TryGetValue(id, out var r) ? r ?? DateTime.MinValue : DateTime.MinValue);

                var unreadCounts = await _uow.MessageRepository.GetAll()
                    .Where(m => chatIds.Contains(m.ChatId) && m.SenderId != me)
                    .GroupBy(m => m.ChatId)
                    .Select(g => new { ChatId = g.Key, Messages = g.Select(m => new { m.SentAt }).ToList() })
                    .ToListAsync();

                var unreadMap = unreadCounts.ToDictionary(
                    g => g.ChatId,
                    g => g.Messages.Count(m => m.SentAt > minReadTimes[g.ChatId]));

                // Batch-fetch peer user data for all DM chats in one query
                var peerIds = rows
                    .Where(r => !r.Chat.IsGroup)
                    .Select(r => r.Chat.Members.Select(m => m.UserId).FirstOrDefault(uid => uid != me))
                    .Where(uid => uid != Guid.Empty)
                    .Distinct()
                    .ToArray();

                Dictionary<Guid, (string Username, string? AvatarUrl)> peerMap;
                if (peerIds.Length > 0)
                {
                    peerMap = (await _uow.UserRepository.GetAll()
                        .Where(u => peerIds.Contains(u.Id))
                        .Select(u => new { u.Id, u.Username, u.AvatarUrl })
                        .ToListAsync())
                        .ToDictionary(u => u.Id, u => (u.Username, u.AvatarUrl));
                }
                else
                {
                    peerMap = [];
                }

                var items = rows.Select(r =>
                {
                    Guid? peerId = null;
                    string? peerName = null;
                    string? peerAvatar = null;

                    if (!r.Chat.IsGroup)
                    {
                        peerId = r.Chat.Members.Select(m => m.UserId).FirstOrDefault(uid => uid != me);
                        if (peerId.HasValue && peerId != Guid.Empty && peerMap.TryGetValue(peerId.Value, out var peer))
                        {
                            peerName = peer.Username;
                            peerAvatar = peer.AvatarUrl;
                        }
                    }

                    var unread = unreadMap.TryGetValue(r.Chat.Id, out var cnt) ? cnt : 0;

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

                _logger.LogDebug("Fetched {Count} chats for user {UserId}", items.Count, me);
                return new PagedResult<ChatListItemDto>
                {
                    Items = items,
                    HasMore = items.Count == take,
                    NextBefore = items.Count > 0 ? items.Last().LastActivityAt : before
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch chats for user {UserId}", me);
                throw;
            }
        }

        public async Task<DirectChatHeaderDto?> GetChatHeaderByChatIdAsync(Guid me, Guid chatId)
        {
            try
            {
                var chat = await _uow.ChatRepository
                    .GetAll(nameof(Chat.Members))
                    .FirstOrDefaultAsync(c => c.Id == chatId);
                if (chat == null) return null;

                if (!chat.IsGroup)
                {
                    var otherId = chat.Members.Select(m => m.UserId).FirstOrDefault(uid => uid != me);
                    if (otherId == Guid.Empty) return null;

                    var other = await _uow.UserRepository
                        .GetAll()
                        .Where(u => u.Id == otherId)
                        .Select(UserProjections.ToProfileDto)
                        .FirstOrDefaultAsync();
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch chat header for chat {ChatId}, user {UserId}", chatId, me);
                throw;
            }
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
                .Select(UserProjections.ToProfileDto)
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("User not found");

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

            _logger.LogDebug("Fetching messages for chat {ChatId}, user {UserId}, limit={Limit}", chatId, me, limit);

            try
            {
                var myLastReadAt = await _uow.ChatMemberRepository.GetAll()
                    .Where(cm => cm.ChatId == chatId && cm.UserId == me)
                    .Select(cm => cm.LastReadAt)
                    .FirstOrDefaultAsync() ?? DateTime.MinValue;

                var q = _uow.MessageRepository.GetAll().Where(m => m.ChatId == chatId);
                if (before.HasValue) q = q.Where(m => m.SentAt < before.Value);

                var take = Math.Clamp(limit, 1, 200);

                var items = await q
                    .OrderByDescending(m => m.SentAt)
                    .Take(take)
                    .Select(MessageProjections.ToChatMessageDto)
                    .ToListAsync();

                items.Reverse();

                _logger.LogDebug("Fetched {Count} messages for chat {ChatId}", items.Count, chatId);
                return new PagedResult<ChatMessageDto>
                {
                    Items = items,
                    HasMore = items.Count == take,
                    NextBefore = items.Count > 0 ? items.First().SentAt : before
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch messages for chat {ChatId}, user {UserId}", chatId, me);
                throw;
            }
        }

        public async Task<SendMessageResponse> SendMessageToUserAsync(Guid me, Guid other, string content, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content is required");

            var chatId = await GetDirectChatIdAsync(me, other);
            if (!chatId.HasValue)
            {
                var chat = new Chat { IsGroup = false };
                await _uow.ChatRepository.InsertAsync(chat, ct);
                var cm1 = new ChatMember { Chat = chat, UserId = me };
                var cm2 = new ChatMember { Chat = chat, UserId = other };
                await _uow.ChatMemberRepository.CreateRangeAsync([cm1, cm2], ct);
                await _uow.CommitAsync(ct);
                chatId = chat.Id;
                _logger.LogInformation("Created new direct chat {ChatId} between {UserId} and {OtherId}", chatId, me, other);
            }

            var message = new Message
            {
                ChatId = chatId.Value,
                SenderId = me,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            await _uow.MessageRepository.InsertAsync(message, ct);
            await _uow.CommitAsync(ct);

            _logger.LogDebug("Message {MessageId} sent to chat {ChatId} by user {SenderId}", message.Id, chatId, me);

            var dto = MessageProjections.ToChatMessageDtoFunc(message);

            await _events.MessageCreatedAsync(chatId.Value, dto, other, ct);

            return new SendMessageResponse
            {
                ChatId = chatId.Value,
                Message = dto
            };
        }

        public async Task<ReadStateDto> MarkChatReadAsync(Guid me, Guid chatId, DateTime? atUtc = null)
        {
            try
            {
                var cm = await _uow.ChatMemberRepository
                    .GetAll()
                    .FirstOrDefaultAsync(x => x.ChatId == chatId && x.UserId == me)
                    ?? throw new UnauthorizedAccessException("Not a member of this chat");

                var now = atUtc?.ToUniversalTime() ?? DateTime.UtcNow;
                cm.LastReadAt = cm.LastReadAt.HasValue && cm.LastReadAt.Value > now ? cm.LastReadAt : now;
                await _uow.ChatMemberRepository.UpdateAsync(cm);
                await _uow.CommitAsync();

                var unread = await _uow.MessageRepository.GetAll()
                    .Where(m => m.ChatId == chatId && m.SenderId != me && m.SentAt > (cm.LastReadAt ?? DateTime.MinValue))
                    .CountAsync();

                _logger.LogDebug("Chat {ChatId} marked as read by user {UserId}, unread={Unread}", chatId, me, unread);
                return new ReadStateDto
                {
                    ChatId = chatId,
                    UserId = me,
                    LastReadAt = cm.LastReadAt ?? DateTime.MinValue,
                    UnreadCount = unread
                };
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark chat {ChatId} as read for user {UserId}", chatId, me);
                throw;
            }
        }

        public async Task<ReadStateDto> GetReadStateAsync(Guid me, Guid chatId)
        {
            try
            {
                var cm = await _uow.ChatMemberRepository
                    .GetAll()
                    .FirstOrDefaultAsync(x => x.ChatId == chatId && x.UserId == me)
                    ?? throw new UnauthorizedAccessException("Not a member of this chat");

                var unread = await _uow.MessageRepository.GetAll()
                    .Where(m => m.ChatId == chatId && m.SenderId != me && m.SentAt > (cm.LastReadAt ?? DateTime.MinValue))
                    .CountAsync();

                return new ReadStateDto
                {
                    ChatId = chatId,
                    UserId = me,
                    LastReadAt = cm.LastReadAt ?? DateTime.MinValue,
                    UnreadCount = unread
                };
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get read state for chat {ChatId}, user {UserId}", chatId, me);
                throw;
            }
        }

        public async Task<Guid?> TryGetDirectPeerAsync(Guid chatId, Guid me, CancellationToken ct = default)
        {
            var chatInfo = await _uow.ChatRepository.GetAll()
                .AsNoTracking()
                .Where(c => c.Id == chatId)
                .Select(c => new { c.Id, c.IsGroup })
                .SingleOrDefaultAsync(ct);

            if (chatInfo is null || chatInfo.IsGroup)
                return null;

            var members = await _uow.ChatMemberRepository.GetAll()
                .AsNoTracking()
                .Where(m => m.ChatId == chatId)
                .Select(m => m.UserId)
                .Take(3)
                .ToListAsync(ct);

            if (members.Count != 2) return null;
            if (!members.Contains(me)) return null;

            var peer = members[0] == me ? members[1] : members[0];
            return peer;
        }
    }
}

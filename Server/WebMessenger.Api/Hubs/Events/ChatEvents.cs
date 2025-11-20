using Microsoft.AspNetCore.SignalR;
using WebMessenger.Api.Hubs.Events.Interfaces;
using WebMessenger.Api.Hubs.Helpers;
using WebMessenger.Api.Models;

namespace WebMessenger.Api.Hubs.Events;

public sealed class ChatEvents(IHubContext<ChatHub> hub, ILogger<ChatEvents> logger) : IChatEvents
{
    private readonly IHubContext<ChatHub> _hub = hub;
    private readonly ILogger<ChatEvents> _log = logger;

    public async Task MessageCreatedAsync(Guid chatId, ChatMessageDto message, Guid? peerUserId = null, CancellationToken ct = default)
    {
        var payload = new
        {
            chatId,
            peerUserId,
            message
        };

        if (chatId == Guid.Empty && peerUserId.HasValue)
        {
            var (a, b) = message.SenderId.CompareTo(peerUserId.Value) <= 0
                ? (message.SenderId, peerUserId.Value)
                : (peerUserId.Value, message.SenderId);

            await _hub.Clients.Group(SignalRGroups.Direct(a, b))
                .SendAsync(Helpers.Events.MessageCreated, payload, ct);

            await _hub.Clients.Group(SignalRGroups.User(message.SenderId))
                .SendAsync(Helpers.Events.MessageCreated, payload, ct);
            await _hub.Clients.Group(SignalRGroups.User(peerUserId.Value))
                .SendAsync(Helpers.Events.MessageCreated, payload, ct);

            _log.LogTrace("MessageCreated -> DM group {Group} + user groups for message {MessageId}", SignalRGroups.Direct(a, b), message.Id);
        }
        else
        {
            await _hub.Clients.Group(SignalRGroups.Chat(chatId))
                .SendAsync(Helpers.Events.MessageCreated, payload, ct);

            await _hub.Clients.Group(SignalRGroups.User(message.SenderId))
                .SendAsync(Helpers.Events.MessageCreated, payload, ct);
            if (peerUserId.HasValue)
            {
                await _hub.Clients.Group(SignalRGroups.User(peerUserId.Value))
                    .SendAsync(Helpers.Events.MessageCreated, payload, ct);
            }

            _log.LogTrace("MessageCreated -> Chat group {Group} + user groups for message {MessageId}", SignalRGroups.Chat(chatId), message.Id);
        }
    }

    public async Task ReadReceiptAsync(Guid chatId, Guid userId, DateTime lastReadAtUtc, CancellationToken ct = default)
    {
        var payload = new { chatId, userId, lastReadAt = lastReadAtUtc };
        await _hub.Clients.Group(SignalRGroups.Chat(chatId)).SendAsync(Helpers.Events.ReadReceipt, payload, ct);
        _log.LogTrace("ReadReceipt -> {Group} upTo {LastReadAt} by {UserId}", SignalRGroups.Chat(chatId), lastReadAtUtc, userId);
    }

    public async Task TypingAsync(Guid chatId, Guid userId, bool isTyping, CancellationToken ct = default)
    {
        var payload = new { chatId, userId, isTyping };
        await _hub.Clients.Group(SignalRGroups.Chat(chatId)).SendAsync(Helpers.Events.Typing, payload, ct);
        _log.LogTrace("Typing -> {Group} {UserId} {IsTyping}", SignalRGroups.Chat(chatId), userId, isTyping);
    }
}
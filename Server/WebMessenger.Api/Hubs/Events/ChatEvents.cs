using Microsoft.AspNetCore.SignalR;
using WebMessenger.Api.Hubs.Events.Interfaces;
using WebMessenger.Api.Hubs.Interfaces;
using WebMessenger.Contracts.Helpers;
using WebMessenger.Contracts.Models;

namespace WebMessenger.Api.Hubs.Events;

public sealed class ChatEvents(IHubContext<ChatHub, IChatClient> hub, ILogger<ChatEvents> logger) : IChatEvents
{
    private readonly IHubContext<ChatHub, IChatClient> _hub = hub;
    private readonly ILogger<ChatEvents> _log = logger;

    public async Task MessageCreatedAsync(Guid chatId, ChatMessageDto message, Guid? peerUserId = null, CancellationToken ct = default)
    {
        var payload = new MessageCreatedPayload(chatId, peerUserId, message);

        if (chatId == Guid.Empty && peerUserId.HasValue)
        {
            var dmGroup = SignalRGroups.Direct(message.SenderId, peerUserId.Value);

            await _hub.Clients.Group(dmGroup).MessageCreated(payload, ct);
            await _hub.Clients.Group(SignalRGroups.User(message.SenderId)).MessageCreated(payload, ct);
            await _hub.Clients.Group(SignalRGroups.User(peerUserId.Value)).MessageCreated(payload, ct);

            _log.LogTrace("MessageCreated -> DM group {Group} + user groups for message {MessageId}", dmGroup, message.Id);
        }
        else
        {
            var chatGroup = SignalRGroups.Chat(chatId);

            await _hub.Clients.Group(chatGroup).MessageCreated(payload, ct);
            await _hub.Clients.Group(SignalRGroups.User(message.SenderId)).MessageCreated(payload, ct);
            if (peerUserId.HasValue)
                await _hub.Clients.Group(SignalRGroups.User(peerUserId.Value)).MessageCreated(payload, ct);

            _log.LogTrace("MessageCreated -> Chat group {Group} + user groups for message {MessageId}", chatGroup, message.Id);
        }
    }

    public async Task ReadReceiptAsync(Guid chatId, Guid userId, DateTime lastReadAtUtc, CancellationToken ct = default)
    {
        var payload = new ReadReceiptPayload(chatId, userId, lastReadAtUtc);
        var chatGroup = SignalRGroups.Chat(chatId);
        await _hub.Clients.Group(chatGroup).ReadReceipt(payload, ct);
        _log.LogTrace("ReadReceipt -> {Group} upTo {LastReadAt} by {UserId}", chatGroup, lastReadAtUtc, userId);
    }

    public async Task TypingAsync(Guid chatId, Guid userId, bool isTyping, string? excludeConnectionId = null, CancellationToken ct = default)
    {
        var payload = new TypingPayload(chatId, userId, isTyping);
        var chatGroup = SignalRGroups.Chat(chatId);

        var target = excludeConnectionId is null
            ? _hub.Clients.Group(chatGroup)
            : _hub.Clients.GroupExcept(chatGroup, excludeConnectionId);

        await target.Typing(payload, ct);
        _log.LogTrace("Typing -> {Group} {UserId} {IsTyping}", chatGroup, userId, isTyping);
    }
}

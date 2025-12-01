using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Contracts.Helpers;

namespace WebMessenger.Api.Hubs;

#nullable enable
[Authorize]
public class ChatHub(IChatService chats, ILogger<ChatHub> logger) : Hub
{
    private readonly IChatService _chats = chats;
    private readonly ILogger<ChatHub> _logger = logger;

    private sealed record TypingPayload(Guid ChatId, Guid UserId, bool IsTyping);
    private sealed record ReadReceiptPayload(Guid ChatId, Guid UserId, DateTime LastReadAt);

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.User(userId.Value));
        _logger.LogInformation("User {UserId} connected, conn {ConnId}", userId, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Disconnected {ConnId}. Reason: {Reason}", Context.ConnectionId, exception?.Message);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinChat(Guid chatId)
    {
        var me = GetCurrentUserIdOrThrow();
        _ = await _chats.GetMessagesAsync(me, chatId, limit: 1, before: null);

        try
        {
            var peer = await _chats.TryGetDirectPeerAsync(chatId, me);
            if (peer.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalRGroups.Direct(me, peer.Value));
                _logger.LogDebug("Conn {ConnId} auto-left dm group {Group}", Context.ConnectionId, SignalRGroups.Direct(me, peer.Value));
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Auto-leave DM failed for chat {ChatId}", chatId);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.Chat(chatId));
        _logger.LogDebug("Conn {ConnId} joined chat group {Group}", Context.ConnectionId, SignalRGroups.Chat(chatId));
    }

    public async Task JoinDirect(Guid otherUserId)
    {
        var me = GetCurrentUserIdOrThrow();
        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.Direct(me, otherUserId));
        _logger.LogDebug("Conn {ConnId} joined dm group {Group}", Context.ConnectionId, SignalRGroups.Direct(me, otherUserId));
    }

    public async Task Typing(Guid chatId, bool isTyping)
    {
        var me = GetCurrentUserId();
        if (!me.HasValue) return;

        await Clients.Group(SignalRGroups.Chat(chatId))
            .SendAsync(Contracts.Helpers.Events.Typing, new TypingPayload(chatId, me.Value, isTyping));

        _logger.LogTrace("Typing: user {UserId} -> chat {ChatId}: {IsTyping}", me.Value, chatId, isTyping);
    }

    public async Task MarkRead(Guid chatId, DateTime upToUtc)
    {
        var me = GetCurrentUserId();
        if (!me.HasValue) return;

        await Clients.Group(SignalRGroups.Chat(chatId))
            .SendAsync(Contracts.Helpers.Events.ReadReceipt, new ReadReceiptPayload(chatId, me.Value, upToUtc));

        _logger.LogTrace("ReadReceipt (hub-only): user {UserId} upTo {UpTo} in chat {ChatId}", me.Value, upToUtc, chatId);
    }

    public async Task LeaveChat(Guid chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalRGroups.Chat(chatId));
        _logger.LogDebug("Conn {ConnId} left chat group {Group}", Context.ConnectionId, SignalRGroups.Chat(chatId));
    }

    public async Task LeaveDirect(Guid otherUserId)
    {
        var me = GetCurrentUserIdOrThrow();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalRGroups.Direct(me, otherUserId));
        _logger.LogDebug("Conn {ConnId} left dm group {Group}", Context.ConnectionId, SignalRGroups.Direct(me, otherUserId));
    }

    private Guid? GetCurrentUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private Guid GetCurrentUserIdOrThrow()
    {
        var id = GetCurrentUserId();
        if (!id.HasValue) throw new HubException("Unauthorized");
        return id.Value;
    }
}
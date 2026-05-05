using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using WebMessenger.Api.Hubs.Events.Interfaces;
using WebMessenger.Api.Hubs.Interfaces;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Contracts.Helpers;
using WebMessenger.Contracts.Models;

namespace WebMessenger.Api.Hubs;

#nullable enable
[Authorize]
public class ChatHub(IChatService chats, IChatEvents chatEvents, ILogger<ChatHub> logger) : Hub<IChatClient>
{
    private readonly IChatService _chats = chats;
    private readonly IChatEvents _chatEvents = chatEvents;
    private readonly ILogger<ChatHub> _logger = logger;

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
        var ct = Context.ConnectionAborted;
        var me = GetCurrentUserIdOrThrow();

        try
        {
            _ = await _chats.GetMessagesAsync(me, chatId, limit: 1, before: null);
        }
        catch (UnauthorizedAccessException)
        {
            throw new HubException("You are not a member of this chat");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed membership check for user {UserId} in chat {ChatId}", me, chatId);
            throw new HubException("Failed to join chat");
        }

        try
        {
            var peer = await _chats.TryGetDirectPeerAsync(chatId, me, ct);
            if (peer.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalRGroups.Direct(me, peer.Value), ct);
                _logger.LogDebug("Conn {ConnId} auto-left dm group {Group}", Context.ConnectionId, SignalRGroups.Direct(me, peer.Value));
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Auto-leave DM failed for chat {ChatId}", chatId);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.Chat(chatId), ct);
        _logger.LogDebug("Conn {ConnId} joined chat group {Group}", Context.ConnectionId, SignalRGroups.Chat(chatId));
    }

    public async Task JoinDirect(Guid otherUserId)
    {
        var ct = Context.ConnectionAborted;
        var me = GetCurrentUserIdOrThrow();

        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.Direct(me, otherUserId), ct);
            _logger.LogDebug("Conn {ConnId} joined dm group {Group}", Context.ConnectionId, SignalRGroups.Direct(me, otherUserId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join DM group for user {UserId} with {OtherId}", me, otherUserId);
            throw new HubException("Failed to join direct conversation");
        }
    }

    public async Task Typing(Guid chatId, bool isTyping)
    {
        var ct = Context.ConnectionAborted;
        var me = GetCurrentUserId();
        if (!me.HasValue) return;

        try
        {
            await _chatEvents.TypingAsync(chatId, me.Value, isTyping, excludeConnectionId: Context.ConnectionId, ct: ct);
            _logger.LogTrace("Typing: user {UserId} -> chat {ChatId}: {IsTyping}", me.Value, chatId, isTyping);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast typing from user {UserId} in chat {ChatId}", me.Value, chatId);
        }
    }

    public async Task MarkRead(Guid chatId, DateTime upToUtc)
    {
        var ct = Context.ConnectionAborted;
        var me = GetCurrentUserId();
        if (!me.HasValue) return;

        try
        {
            await _chatEvents.ReadReceiptAsync(chatId, me.Value, upToUtc, ct);
            _logger.LogTrace("ReadReceipt (hub-only): user {UserId} upTo {UpTo} in chat {ChatId}", me.Value, upToUtc, chatId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast read receipt from user {UserId} in chat {ChatId}", me.Value, chatId);
        }
    }

    public async Task LeaveChat(Guid chatId)
    {
        var ct = Context.ConnectionAborted;
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalRGroups.Chat(chatId), ct);
            _logger.LogDebug("Conn {ConnId} left chat group {Group}", Context.ConnectionId, SignalRGroups.Chat(chatId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to leave chat group {ChatId} for conn {ConnId}", chatId, Context.ConnectionId);
        }
    }

    public async Task LeaveDirect(Guid otherUserId)
    {
        var ct = Context.ConnectionAborted;
        var me = GetCurrentUserIdOrThrow();

        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalRGroups.Direct(me, otherUserId), ct);
            _logger.LogDebug("Conn {ConnId} left dm group {Group}", Context.ConnectionId, SignalRGroups.Direct(me, otherUserId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to leave DM group for user {UserId} with {OtherId}", me, otherUserId);
        }
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

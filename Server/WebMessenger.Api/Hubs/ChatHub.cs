using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WebMessenger.Api.Services.Interfaces;

namespace WebMessenger.Api.Hubs;

#nullable enable

[Authorize]
public class ChatHub(IUserService users, IChatService chats, ILogger<ChatHub> logger) : Hub
{
    private readonly IUserService _users = users;
    private readonly IChatService _chats = chats;
    private readonly ILogger<ChatHub> _logger = logger;

    private static class Events
    {
        public const string MessageCreated = "MessageCreated";
        public const string ReadReceipt = "ReadReceipt";
        public const string Typing = "Typing";
    }

    private static string UG(Guid userId) => $"user:{userId}";
    private static string CG(Guid chatId) => $"chat:{chatId}";
    private static string DM(Guid a, Guid b)
    {
        var (x, y) = a.CompareTo(b) <= 0 ? (a, b) : (b, a);
        return $"dm:{x}:{y}";
    }

    private sealed record TypingPayload(Guid chatId, Guid userId, bool isTyping);
    private sealed record ReadReceiptPayload(Guid chatId, Guid userId, DateTime lastReadAtUtc);

    public override async Task OnConnectedAsync()
    {
        var userId = await GetCurrentUserId();
        if (!userId.HasValue)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, UG(userId.Value));

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
        var me = await GetCurrentUserId();
        if (!me.HasValue) throw new HubException("Unauthorized");

        _ = await _chats.GetMessagesAsync(me.Value, chatId, limit: 1, before: null);

        try
        {
            var peer = await _chats.TryGetDirectPeerAsync(chatId, me.Value);
            if (peer.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, DM(me.Value, peer.Value));
                _logger.LogDebug("Conn {ConnId} auto-left dm group {Group}", Context.ConnectionId, DM(me.Value, peer.Value));
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Auto-leave DM failed for chat {ChatId}", chatId);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, CG(chatId));
        _logger.LogDebug("Conn {ConnId} joined chat group {Group}", Context.ConnectionId, CG(chatId));
    }

    public async Task JoinDirect(Guid otherUserId)
    {
        var me = await GetCurrentUserId();
        if (!me.HasValue) throw new HubException("Unauthorized");

        await Groups.AddToGroupAsync(Context.ConnectionId, DM(me.Value, otherUserId));
        _logger.LogDebug("Conn {ConnId} joined dm group {Group}", Context.ConnectionId, DM(me.Value, otherUserId));
    }

    public async Task Typing(Guid chatId, bool isTyping)
    {
        var me = await GetCurrentUserId();
        if (!me.HasValue) return;

        await Clients.Group(CG(chatId))
            .SendAsync(Events.Typing, new TypingPayload(chatId, me.Value, isTyping));

        _logger.LogTrace("Typing: user {UserId} -> chat {ChatId}: {IsTyping}", me.Value, chatId, isTyping);
    }

    public async Task MarkRead(Guid chatId, DateTime upToUtc)
    {
        var me = await GetCurrentUserId();
        if (!me.HasValue) return;

        await Clients.Group(CG(chatId))
            .SendAsync(Events.ReadReceipt, new ReadReceiptPayload(chatId, me.Value, upToUtc));

        _logger.LogTrace("ReadReceipt (hub-only): user {UserId} upTo {UpTo} in chat {ChatId}", me.Value, upToUtc, chatId);
    }

    private async Task<Guid?> GetCurrentUserId()
    {
        var http = Context.GetHttpContext();
        string? bearer = null;

        if (http?.Request.Cookies.TryGetValue("auth-token", out var jwtFromCookie) == true && !string.IsNullOrWhiteSpace(jwtFromCookie))
        {
            bearer = $"Bearer {jwtFromCookie}";
        }
        else
        {
            var jwtFromQuery = http?.Request.Query["access_token"].ToString();
            if (!string.IsNullOrWhiteSpace(jwtFromQuery))
                bearer = $"Bearer {jwtFromQuery}";
        }

        if (string.IsNullOrWhiteSpace(bearer))
            return null;

        return await _users.GetUserIdFromAuthHeader(bearer);
    }

    public async Task LeaveChat(Guid chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, CG(chatId));
        _logger.LogDebug("Conn {ConnId} left chat group {Group}", Context.ConnectionId, CG(chatId));
    }

    public async Task LeaveDirect(Guid otherUserId)
    {
        var me = await GetCurrentUserId();
        if (!me.HasValue) throw new HubException("Unauthorized");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, DM(me.Value, otherUserId));
        _logger.LogDebug("Conn {ConnId} left dm group {Group}", Context.ConnectionId, DM(me.Value, otherUserId));
    }
}
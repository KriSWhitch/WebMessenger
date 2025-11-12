using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WebMessenger.Api.Services.Interfaces;

namespace WebMessenger.Api.Hubs;

[Authorize]
public class ChatHub(IUserService users, IChatService chats, ILogger<ChatHub> logger) : Hub
{
    private readonly IUserService _users = users;
    private readonly IChatService _chats = chats;
    private readonly ILogger<ChatHub> _logger = logger;

    private static string UG(Guid userId) => $"user:{userId}";
    private static string CG(Guid chatId) => $"chat:{chatId}";
    private static string DM(Guid a, Guid b)
    {
        var (x, y) = a.CompareTo(b) <= 0 ? (a, b) : (b, a);
        return $"dm:{x}:{y}";
    }

    public override async Task OnConnectedAsync()
    {
        var userId = await GetCurrentUserId();
        if (!userId.HasValue)
        {
            Context.Abort();
            return;
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, UG(userId.Value));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinChat(Guid chatId)
    {
        var me = await GetCurrentUserId();
        if (!me.HasValue) throw new HubException("Unauthorized");

        _ = await _chats.GetMessagesAsync(me.Value, chatId, 1, null);
        await Groups.AddToGroupAsync(Context.ConnectionId, CG(chatId));
    }

    public async Task JoinDirect(Guid otherUserId)
    {
        var me = await GetCurrentUserId();
        if (!me.HasValue) throw new HubException("Unauthorized");
        await Groups.AddToGroupAsync(Context.ConnectionId, DM(me.Value, otherUserId));
    }

    public async Task Typing(Guid chatId, bool isTyping)
    {
        var me = await GetCurrentUserId();
        if (!me.HasValue) return;
        await Clients.Group(CG(chatId)).SendAsync("Typing", new { chatId, userId = me.Value, isTyping });
    }

    public async Task MarkRead(Guid chatId, DateTime upToUtc)
    {
        var me = await GetCurrentUserId();
        if (!me.HasValue) return;
        await Clients.Group(CG(chatId)).SendAsync("Read", new { chatId, userId = me.Value, upToUtc });
    }

    private async Task<Guid?> GetCurrentUserId()
    {
        var token = Context.GetHttpContext()?.Request.Cookies.TryGetValue("auth-token", out var jwt) == true
            ? $"Bearer {jwt}"
            : $"Bearer {Context.GetHttpContext()?.Request.Query["access_token"]}";

        if (string.IsNullOrWhiteSpace(token)) return null;
        return await _users.GetUserIdFromAuthHeader(token);
    }
}
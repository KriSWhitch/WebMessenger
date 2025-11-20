using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebMessenger.Api.Models;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Api.Hubs.Events.Interfaces;
using WebMessenger.Api.Infrastructure.Interfaces;

namespace WebMessenger.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/chats")]
    public class ChatController(
        ICurrentUser currentUser,
        IChatService chatService,
        ILogger<ChatController> logger,
        IChatEvents chatEvents) : ControllerBase
    {
        private readonly ICurrentUser _currentUser = currentUser;
        private readonly IChatService _chatService = chatService;
        private readonly ILogger<ChatController> _logger = logger;
        private readonly IChatEvents _events = chatEvents;

        [HttpGet]
        public async Task<ActionResult<PagedResult<ChatListItemDto>>> Index(int limit = 20, DateTime? before = null, CancellationToken ct = default)
        {
            var page = await _chatService.GetUserChatsAsync(_currentUser.Id, limit, before);
            return Ok(page);
        }

        [HttpGet("{chatId:guid}/header")]
        public async Task<IActionResult> GetChatHeader(Guid chatId, CancellationToken ct)
        {
            var dto = await _chatService.GetChatHeaderByChatIdAsync(_currentUser.Id, chatId);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpGet("direct/{userId:guid}/header")]
        public async Task<IActionResult> GetDirectHeader(Guid userId, CancellationToken ct)
        {
            var dto = await _chatService.GetDirectChatHeaderAsync(_currentUser.Id, userId);
            return Ok(dto);
        }

        [HttpGet("{chatId:guid}/messages")]
        public async Task<ActionResult<PagedResult<ChatMessageDto>>> GetMessages(Guid chatId, int limit = 50, DateTime? before = null, CancellationToken ct = default)
        {
            var page = await _chatService.GetMessagesAsync(_currentUser.Id, chatId, limit, before);
            return Ok(page);
        }

        [HttpPost("direct/{userId:guid}/messages")]
        public async Task<ActionResult<SendMessageResponse>> SendMessage(Guid userId, [FromBody] SendMessageRequest req, CancellationToken ct)
        {
            var result = await _chatService.SendMessageToUserAsync(_currentUser.Id, userId, req.Content);
            return Ok(result);
        }

        [HttpPost("{chatId:guid}/read")]
        public async Task<IActionResult> MarkRead(Guid chatId, [FromBody] MarkReadRequest? body, CancellationToken ct)
        {
            DateTime? at = body?.At?.Kind switch
            {
                DateTimeKind.Utc => body.At,
                DateTimeKind.Local => body.At?.ToUniversalTime(),
                DateTimeKind.Unspecified => body.At.HasValue ? DateTime.SpecifyKind(body.At.Value, DateTimeKind.Utc) : null,
                _ => null
            };

            var state = await _chatService.MarkChatReadAsync(_currentUser.Id, chatId, at);
            await _events.ReadReceiptAsync(chatId, _currentUser.Id, state.LastReadAt, ct);
            return Ok(new { lastReadAt = state.LastReadAt, unreadCount = state.UnreadCount });
        }

        [HttpGet("{chatId:guid}/read-state")]
        public async Task<IActionResult> GetReadState(Guid chatId, CancellationToken ct)
        {
            var state = await _chatService.GetReadStateAsync(_currentUser.Id, chatId);
            return Ok(new { lastReadAt = state.LastReadAt, unreadCount = state.UnreadCount });
        }
    }
}
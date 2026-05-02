using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Api.Hubs.Events.Interfaces;
using WebMessenger.Api.Infrastructure.Interfaces;
using WebMessenger.Contracts.Models;

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
        public async Task<ActionResult<PagedResult<ChatListItemDto>>> Index(int limit = 20, DateTime? before = null)
        {
            try
            {
                var page = await _chatService.GetUserChatsAsync(_currentUser.Id, limit, before);
                return Ok(page);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching chats for user {UserId}", _currentUser.Id);
                return StatusCode(500, new { message = "An error occurred while fetching chats" });
            }
        }

        [HttpGet("{chatId:guid}/header")]
        public async Task<IActionResult> GetChatHeader(Guid chatId)
        {
            try
            {
                var dto = await _chatService.GetChatHeaderByChatIdAsync(_currentUser.Id, chatId);
                return dto is null ? NotFound() : Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching header for chat {ChatId}, user {UserId}", chatId, _currentUser.Id);
                return StatusCode(500, new { message = "An error occurred while fetching chat header" });
            }
        }

        [HttpGet("direct/{userId:guid}/header")]
        public async Task<IActionResult> GetDirectHeader(Guid userId)
        {
            try
            {
                var dto = await _chatService.GetDirectChatHeaderAsync(_currentUser.Id, userId);
                return Ok(dto);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "User {OtherUserId} not found while fetching direct header for {UserId}", userId, _currentUser.Id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching direct header with user {OtherUserId} for {UserId}", userId, _currentUser.Id);
                return StatusCode(500, new { message = "An error occurred while fetching chat header" });
            }
        }

        [HttpGet("{chatId:guid}/messages")]
        public async Task<ActionResult<PagedResult<ChatMessageDto>>> GetMessages(Guid chatId, int limit = 50, DateTime? before = null)
        {
            try
            {
                var page = await _chatService.GetMessagesAsync(_currentUser.Id, chatId, limit, before);
                return Ok(page);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "User {UserId} attempted to access messages in chat {ChatId} without membership", _currentUser.Id, chatId);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching messages for chat {ChatId}, user {UserId}", chatId, _currentUser.Id);
                return StatusCode(500, new { message = "An error occurred while fetching messages" });
            }
        }

        [HttpPost("direct/{userId:guid}/messages")]
        public async Task<ActionResult<SendMessageResponse>> SendMessage(Guid userId, [FromBody] SendMessageRequest req, CancellationToken ct)
        {
            try
            {
                var result = await _chatService.SendMessageToUserAsync(_currentUser.Id, userId, req.Content, ct);
                _logger.LogDebug("User {SenderId} sent message to user {ReceiverId}", _currentUser.Id, userId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid message content from user {UserId}", _currentUser.Id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message from user {SenderId} to user {ReceiverId}", _currentUser.Id, userId);
                return StatusCode(500, new { message = "An error occurred while sending the message" });
            }
        }

        [HttpPost("{chatId:guid}/read")]
        public async Task<IActionResult> MarkRead(Guid chatId, [FromBody] MarkReadRequest? body, CancellationToken ct)
        {
            try
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
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "User {UserId} attempted to mark read in chat {ChatId} without membership", _currentUser.Id, chatId);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking chat {ChatId} as read for user {UserId}", chatId, _currentUser.Id);
                return StatusCode(500, new { message = "An error occurred while marking chat as read" });
            }
        }

        [HttpGet("{chatId:guid}/read-state")]
        public async Task<IActionResult> GetReadState(Guid chatId)
        {
            try
            {
                var state = await _chatService.GetReadStateAsync(_currentUser.Id, chatId);
                return Ok(new { lastReadAt = state.LastReadAt, unreadCount = state.UnreadCount });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "User {UserId} attempted to get read state of chat {ChatId} without membership", _currentUser.Id, chatId);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching read state for chat {ChatId}, user {UserId}", chatId, _currentUser.Id);
                return StatusCode(500, new { message = "An error occurred while fetching read state" });
            }
        }
    }
}
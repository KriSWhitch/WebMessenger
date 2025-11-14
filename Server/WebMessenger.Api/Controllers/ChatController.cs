using Microsoft.AspNetCore.Mvc;
using WebMessenger.Api.Models;
using WebMessenger.Api.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using WebMessenger.Api.Hubs;

namespace WebMessenger.Api.Controllers
{
    [ApiController]
    [Route("api/chats")]
    public class ChatController(IUserService userService, IChatService chatService, ILogger<ChatController> logger, IHubContext<ChatHub> hub) : ControllerBase
    {
        private readonly IUserService _userService = userService;
        private readonly IChatService _chatService = chatService;
        private readonly ILogger<ChatController> _logger = logger;
        private readonly IHubContext<ChatHub> _hub = hub;

        [HttpGet]
        public async Task<IActionResult> Index(
            [FromHeader(Name = "Authorization")] string auth,
            int limit = 20,
            DateTime? before = null)
        {
            var me = await _userService.GetUserIdFromAuthHeader(auth);
            if (!me.HasValue) return Unauthorized();
            var page = await _chatService.GetUserChatsAsync(me.Value, limit, before);
            return Ok(page);
        }


        [HttpGet("{chatId:guid}/header")]
        public async Task<IActionResult> GetChatHeader(
            [FromHeader(Name = "Authorization")] string auth,
            Guid chatId)
        {
            try
            {
                var me = await _userService.GetUserIdFromAuthHeader(auth);
                if (!me.HasValue) return Unauthorized();

                var dto = await _chatService.GetChatHeaderByChatIdAsync(me.Value, chatId);
                if (dto == null) return NotFound();

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat header by chatId");
                return StatusCode(500, "Error getting chat header");
            }
        }

        [HttpGet("direct/{userId:guid}/header")]
        public async Task<IActionResult> GetDirectHeader(
            [FromHeader(Name = "Authorization")] string auth,
            Guid userId)
        {
            try
            {
                var me = await _userService.GetUserIdFromAuthHeader(auth);
                if (!me.HasValue) return Unauthorized();
                var dto = await _chatService.GetDirectChatHeaderAsync(me.Value, userId);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting direct chat header");
                return StatusCode(500, "Error getting direct chat header");
            }
        }

        [HttpGet("{chatId:guid}/messages")]
        public async Task<IActionResult> GetMessages(
            [FromHeader(Name = "Authorization")] string auth,
            Guid chatId,
            int limit = 50,
            DateTime? before = null)
        {
            try
            {
                var me = await _userService.GetUserIdFromAuthHeader(auth);
                if (!me.HasValue) return Unauthorized();
                var page = await _chatService.GetMessagesAsync(me.Value, chatId, limit, before);
                return Ok(page);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages");
                return StatusCode(500, "Error getting messages");
            }
        }

        [HttpPost("direct/{userId:guid}/messages")]
        public async Task<IActionResult> SendMessage(
            [FromHeader(Name = "Authorization")] string auth,
            Guid userId,
            [FromBody] SendMessageRequest req)
        {
            try
            {
                var me = await _userService.GetUserIdFromAuthHeader(auth);
                if (!me.HasValue) return Unauthorized();

                var result = await _chatService.SendMessageToUserAsync(me.Value, userId, req.Content);

                var payload = new
                {
                    chatId = result.ChatId,
                    message = result.Message
                };

                await _hub.Clients.Group($"chat:{result.ChatId}")
                    .SendAsync("MessageCreated", payload);
                await _hub.Clients.Group($"user:{userId}")
                    .SendAsync("MessageCreated", payload);
                await _hub.Clients.Group($"user:{me.Value}")
                    .SendAsync("MessageCreated", payload);

                return Ok(result);
            }
            catch (ArgumentException aex)
            {
                return BadRequest(aex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return StatusCode(500, "Error sending message");
            }
        }
    }
}
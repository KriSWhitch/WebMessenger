using Microsoft.AspNetCore.Mvc;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Services.Interfaces;

namespace WebMessenger.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController(ILogger<ContactController> logger,
                          IUserService userService) : ControllerBase
    {
        private readonly ILogger<ContactController> _logger = logger;
        private readonly IUserService _userService = userService;

        [HttpGet]
        public async Task<IActionResult> Index([FromHeader(Name = "Authorization")] string authHeader, [FromQuery] string query = "", [FromQuery] int limit = 10)
        {
            try
            {
                var currentUserId = await _userService.GetUserIdFromAuthHeader(authHeader);
                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest("Search query is required");

                if (!currentUserId.HasValue)
                    return BadRequest("Authentication problems occurred");

                var users = await _userService.SearchUsersAsync(currentUserId.Value, query, limit);

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users");
                return StatusCode(500, "An error occurred while searching users");
            }
        }
    }
}

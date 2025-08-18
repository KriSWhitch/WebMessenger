using Microsoft.AspNetCore.Mvc;
using WebMessenger.Api.Models;
using WebMessenger.Api.Services.Interfaces;

namespace WebMessenger.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController(ILogger<UserController> logger,
                          IUserService userService,
                          IAvatarService avatarService) : ControllerBase
    {
        private readonly ILogger<UserController> _logger = logger;
        private readonly IUserService _userService = userService;
        private readonly IAvatarService _avatarService = avatarService;

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

        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile([FromHeader(Name = "Authorization")] string authHeader)
        {
            try
            {
                var userId = await _userService.GetUserIdFromAuthHeader(authHeader);
                if (!userId.HasValue)
                    return Unauthorized();

                var profile = _userService.GetUserProfile(userId.Value);
                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, "An error occurred while getting user profile");
            }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateUserProfile(
            [FromHeader(Name = "Authorization")] string authHeader,
            [FromBody] UpdateProfileDto updateDto)
        {
            try
            {
                var userId = await _userService.GetUserIdFromAuthHeader(authHeader);
                if (!userId.HasValue)
                    return Unauthorized();

                var result = await _userService.UpdateUserProfileAsync(userId.Value, updateDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, "An error occurred while updating user profile");
            }
        }

        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar(
            [FromHeader(Name = "Authorization")] string authHeader, 
            IFormFile file)
        {
            try
            {
                var userId = await _userService.GetUserIdFromAuthHeader(authHeader);
                if (!userId.HasValue)
                    return Unauthorized();

                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                if (!file.ContentType.StartsWith("image/"))
                    return BadRequest("Only image files are allowed");

                if (file.Length > 5 * 1024 * 1024)
                    return BadRequest("File size exceeds 5MB limit");

                var avatarUrl = await _avatarService.UpdateUserAvatarAsync(userId.Value, file);

                return Ok(new { avatarUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar");
                return StatusCode(500, "An error occurred while uploading avatar");
            }
        }
    }
}

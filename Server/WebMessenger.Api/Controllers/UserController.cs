using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebMessenger.Api.Infrastructure.Interfaces;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Contracts.Models;

namespace WebMessenger.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/users")]
    public class UserController(
        ILogger<UserController> logger,
        IUserService userService,
        IAvatarService avatarService,
        ICurrentUser currentUser) : ControllerBase
    {
        private readonly ILogger<UserController> _logger = logger;
        private readonly IUserService _userService = userService;
        private readonly IAvatarService _avatarService = avatarService;
        private readonly ICurrentUser _currentUser = currentUser;

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string query = "", [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Search query is required");

            try
            {
                var users = await _userService.SearchUsersAsync(_currentUser.Id, query, limit);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with query {Query} for user {UserId}", query, _currentUser.Id);
                return StatusCode(500, new { message = "An error occurred while searching users" });
            }
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            try
            {
                var profile = await _userService.GetUserProfileAsync(_currentUser.Id);
                return Ok(profile);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Profile not found for user {UserId}", _currentUser.Id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching profile for user {UserId}", _currentUser.Id);
                return StatusCode(500, new { message = "An error occurred while fetching profile" });
            }
        }

        [HttpGet("profile/{id:guid}")]
        public async Task<IActionResult> GetUserProfileById(Guid id)
        {
            try
            {
                var profile = await _userService.GetUserProfileAsync(id);
                return Ok(profile);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Profile not found for user {UserId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching profile for user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while fetching profile" });
            }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateProfileDto updateDto)
        {
            try
            {
                var result = await _userService.UpdateUserProfileAsync(_currentUser.Id, updateDto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "User not found during profile update for {UserId}", _currentUser.Id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", _currentUser.Id);
                return StatusCode(500, new { message = "An error occurred while updating profile" });
            }
        }

        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");
            if (!file.ContentType.StartsWith("image/"))
                return BadRequest("Only image files are allowed");
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("File size exceeds 5MB limit");

            try
            {
                var avatarUrl = await _avatarService.UpdateUserAvatarAsync(_currentUser.Id, file);
                if (avatarUrl is null)
                {
                    _logger.LogWarning("Avatar upload failed for user {UserId}", _currentUser.Id);
                    return StatusCode(500, new { message = "An error occurred while uploading avatar" });
                }

                _logger.LogInformation("Avatar updated for user {UserId}", _currentUser.Id);
                return Ok(new { avatarUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error uploading avatar for user {UserId}", _currentUser.Id);
                return StatusCode(500, new { message = "An error occurred while uploading avatar" });
            }
        }
    }
}
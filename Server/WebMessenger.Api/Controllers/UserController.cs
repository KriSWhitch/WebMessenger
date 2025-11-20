using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebMessenger.Api.Infrastructure;
using WebMessenger.Api.Infrastructure.Interfaces;
using WebMessenger.Api.Models;
using WebMessenger.Api.Services.Interfaces;

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

            var users = await _userService.SearchUsersAsync(_currentUser.Id, query, limit);
            return Ok(users);
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            var profile = await _userService.GetUserProfileAsync(_currentUser.Id);
            return Ok(profile);
        }

        [HttpGet("profile/{id:guid}")]
        public async Task<IActionResult> GetUserProfileById(Guid id)
        {
            var profile = await _userService.GetUserProfileAsync(id);
            return Ok(profile);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateProfileDto updateDto)
        {
            var result = await _userService.UpdateUserProfileAsync(_currentUser.Id, updateDto);
            return Ok(result);
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

            var avatarUrl = await _avatarService.UpdateUserAvatarAsync(_currentUser.Id, file);
            return avatarUrl is null ? StatusCode(500, "An error occurred while uploading avatar")
                                     : Ok(new { avatarUrl });
        }
    }
}
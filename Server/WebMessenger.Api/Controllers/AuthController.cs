using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Contracts.Models;

namespace WebMessenger.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(IAuthService authService, IUserService userService, ILogger<AuthController> logger) : ControllerBase
    {
        private readonly IUserService _userService = userService;
        private readonly IAuthService _authService = authService;
        private readonly ILogger<AuthController> _logger = logger;

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (await _userService.IsUsernameExistsAsync(registerDto.Username))
                {
                    _logger.LogWarning("Registration attempt with existing username: {Username}", registerDto.Username);
                    return BadRequest(new { message = "Username already exists" });
                }

                await _userService.RegisterUserAsync(registerDto);
                return Ok(new { message = "Registration successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for {Username}", registerDto.Username);
                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var user = await _userService.FindUserByUsernameAsync(loginDto.Username);
                if (user == null || !_authService.ValidateUserCredentials(user, loginDto.Password))
                {
                    _logger.LogWarning("Failed login attempt for username: {Username}", loginDto.Username);
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                var token = _authService.GenerateJwtToken(user);
                Response.Cookies.Append("auth-token", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(1)
                });

                _logger.LogInformation("User {Username} logged in", user.Username);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for {Username}", loginDto.Username);
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        [Authorize]
        [HttpGet("verify")]
        public IActionResult VerifyToken()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.FindFirstValue(ClaimTypes.Name);
            var expiry = User.FindFirstValue("exp");

            return Ok(new
            {
                valid = true,
                userId,
                username,
                expiry
            });
        }
    }
}
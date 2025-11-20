using Microsoft.AspNetCore.Mvc;
using WebMessenger.Api.Models;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Services.Interfaces;

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
            if (await _userService.IsUsernameExistsAsync(registerDto.Username))
                return BadRequest(new { message = "Username already exists" });

            await _userService.RegisterUserAsync(registerDto);
            return Ok(new { message = "Registration successful" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var user = await _userService.FindUserByUsernameAsync(loginDto.Username);
            if (user == null || !_authService.ValidateUserCredentials(user, loginDto.Password))
                return Unauthorized(new { message = "Invalid credentials" });

            var token = _authService.GenerateJwtToken(user);

            Response.Cookies.Append("auth-token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(1)
            });

            return Ok(new { token });
        }

        [HttpGet("verify")]
        public IActionResult VerifyToken([FromHeader(Name = "Authorization")] string auth)
        {
            return _authService.ValidateJwtToken(auth)
                ? Ok(new { valid = true })
                : Unauthorized(new { valid = false });
        }
    }
}
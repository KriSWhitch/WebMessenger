using Microsoft.AspNetCore.Mvc;
using WebMessenger.Api.Models;
using WebMessenger.Services.Interfaces;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        if (await _authService.IsUsernameExistsAsync(registerDto.Username))
            return BadRequest(new { message = "Username already exists" });

        await _authService.RegisterUserAsync(registerDto);
        return Ok(new { message = "Registration successful" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var user = await _authService.FindUserByUsernameAsync(loginDto.Username);

        if (user == null || !_authService.ValidateUserCredentials(user, loginDto.Password))
            return Unauthorized(new { message = "Invalid credentials" });

        var token = _authService.GenerateJwtToken(user);
        return Ok(new { token });
    }

    [HttpGet("verify")]
    public IActionResult VerifyToken([FromHeader(Name = "Authorization")] string authHeader)
    {
        return _authService.ValidateJwtToken(authHeader)
            ? Ok(new { valid = true })
            : Unauthorized(new { valid = false });
    }
}
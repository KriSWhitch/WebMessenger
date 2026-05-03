using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebMessenger.DAL.Entities;
using BCrypt.Net;
using WebMessenger.Api.Options;
using WebMessenger.Api.Services.Interfaces;

namespace WebMessenger.Api.Services;

public class AuthService(
    IOptions<JwtOptions> jwtOptions,
    ILogger<AuthService> logger) : IAuthService
{
    private const string UsernameClaimNotFound = "Username claim not found in token";

    private readonly JwtOptions _jwt = jwtOptions.Value;
    private readonly ILogger<AuthService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public bool ValidateUserCredentials(User? user, string password)
    {
        if (user == null || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Attempt to validate null user or empty password");
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(
                password,
                user.PasswordHash,
                enhancedEntropy: false,
                hashType: HashType.SHA256);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user credentials for {Username}", user.Username);
            return false;
        }
    }

    public string GenerateJwtToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_jwt.ExpireDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string? GetUsernameFromToken(string authHeader)
    {
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            return null;
        }

        try
        {
            var token = ExtractTokenFromHeader(authHeader);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            return jwtToken.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Name ||
                                   c.Type == JwtRegisteredClaimNames.UniqueName)?
                .Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, UsernameClaimNotFound);
            return null;
        }
    }

    private static string ExtractTokenFromHeader(string authHeader)
    {
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityTokenException("Invalid authorization header format");
        }

        return authHeader["Bearer ".Length..].Trim();
    }
}

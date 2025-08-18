using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WebMessenger.DAL.Entities;
using WebMessenger.DAL.Interfaces;
using BCrypt.Net;
using WebMessenger.Services.Interfaces;

namespace WebMessenger.Api.Services;

public class AuthService(
    IConfiguration config,
    ILogger<AuthService> logger) : IAuthService
{
    private const string TokenValidationError = "Token validation failed";
    private const string UsernameClaimNotFound = "Username claim not found in token";

    private readonly IConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));
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

        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var issuer = _config["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
        var audience = _config["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
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
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(GetTokenLifetime()),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool ValidateJwtToken(string authHeader)
    {
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            _logger.LogWarning("Empty authorization header received");
            return false;
        }

        try
        {
            var token = ExtractTokenFromHeader(authHeader);
            var validationParameters = GetTokenValidationParameters();

            new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, TokenValidationError);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return false;
        }
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

    private TokenValidationParameters GetTokenValidationParameters()
    {
        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var issuer = _config["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
        var audience = _config["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(5) // Allow some leeway for clock differences
        };
    }

    private static string ExtractTokenFromHeader(string authHeader)
    {
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityTokenException("Invalid authorization header format");
        }

        return authHeader["Bearer ".Length..].Trim();
    }

    private int GetTokenLifetime()
    {
        if (int.TryParse(_config["Jwt:TokenLifetimeMinutes"], out var lifetime))
        {
            return lifetime;
        }
        return 1440; // Default 24 hours if not configured
    }
}
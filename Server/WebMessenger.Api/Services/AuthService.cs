using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WebMessenger.DAL.Entities;
using WebMessenger.DAL.Interfaces;
using WebMessenger.Api.Models;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using WebMessenger.Services.Interfaces;

namespace WebMessenger.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;

    public AuthService(IUnitOfWork unitOfWork, IConfiguration config)
    {
        _unitOfWork = unitOfWork;
        _config = config;
    }

    public async Task<bool> IsUsernameExistsAsync(string username)
    {
        return await _unitOfWork.UserRepository.GetAll()
            .AnyAsync(u => u.Username == username);
    }

    public async Task<User> RegisterUserAsync(RegisterDto registerDto)
    {
        var user = new User
        {
            Username = registerDto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password)
        };

        _unitOfWork.UserRepository.Insert(user);
        await _unitOfWork.CommitAsync();

        return user;
    }

    public async Task<User?> FindUserByUsernameAsync(string username)
    {
        return await _unitOfWork.UserRepository.GetAll()
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public bool ValidateUserCredentials(User? user, string password)
    {
        return user != null &&
               BCrypt.Net.BCrypt.Verify(password, user.PasswordHash, false, HashType.SHA256);
    }

    public string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool ValidateJwtToken(string authHeader)
    {
        try
        {
            var token = authHeader.Split(' ').Last();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidAudience = _config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_config["Jwt:Key"]!))
            };

            new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
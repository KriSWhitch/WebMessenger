using WebMessenger.DAL.Entities;
using WebMessenger.Api.Models;

namespace WebMessenger.Services.Interfaces;

public interface IAuthService
{
    Task<bool> IsUsernameExistsAsync(string username);
    Task<User> RegisterUserAsync(RegisterDto registerDto);
    Task<User?> FindUserByUsernameAsync(string username);
    bool ValidateUserCredentials(User? user, string password);
    string GenerateJwtToken(User user);
    bool ValidateJwtToken(string authHeader);
}
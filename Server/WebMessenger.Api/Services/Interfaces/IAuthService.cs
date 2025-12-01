using WebMessenger.DAL.Entities;

namespace WebMessenger.Api.Services.Interfaces;

public interface IAuthService
{
    bool ValidateUserCredentials(User? user, string password);
    string GenerateJwtToken(User user);
    string? GetUsernameFromToken(string authHeader);
}
using WebMessenger.DAL.Entities;

namespace WebMessenger.Api.Services.Interfaces;

public interface IAuthService
{
    bool ValidateUserCredentials(User? user, string password);
    string GenerateJwtToken(User user);
    bool ValidateJwtToken(string authHeader);
    string? GetUsernameFromToken(string authHeader);
}
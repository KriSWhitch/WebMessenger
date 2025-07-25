using WebMessenger.DAL.Entities;
using WebMessenger.Api.Models;

namespace WebMessenger.Services.Interfaces;

public interface IAuthService
{
    bool ValidateUserCredentials(User? user, string password);
    string GenerateJwtToken(User user);
    bool ValidateJwtToken(string authHeader);
    string? GetUsernameFromToken(string authHeader);
}
using WebMessenger.Contracts.Models;
using WebMessenger.DAL.Entities;

namespace WebMessenger.Api.Services.Interfaces
{
    public interface IUserService
    {
        Task<bool> IsUsernameExistsAsync(string username);
        Task<User> RegisterUserAsync(RegisterDto registerDto);
        Task<User?> FindUserByUsernameAsync(string username);
        Task<IEnumerable<UserSearchResultDto>> SearchUsersAsync(Guid currentUserId, string query, int limit);
        Task<Guid?> GetUserIdFromAuthHeader(string authHeader);
        Task<UserProfileDto> GetUserProfileAsync(Guid userId);
        Task<UserProfileDto> UpdateUserProfileAsync(Guid userId, UpdateProfileDto updateDto);
    }
}

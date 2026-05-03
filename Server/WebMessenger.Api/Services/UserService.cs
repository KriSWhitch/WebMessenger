using WebMessenger.DAL.Entities;
using WebMessenger.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using WebMessenger.Api.Projections.Users;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Contracts.Models;

namespace WebMessenger.Api.Services;

public class UserService(IUnitOfWork unitOfWork, IContactsService contactsService, IAuthService authService, ILogger<UserService> logger) : IUserService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IContactsService _contactsService = contactsService;
    private readonly IAuthService _authService = authService;
    private readonly ILogger<UserService> _logger = logger;

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

        await _unitOfWork.UserRepository.InsertAsync(user);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("User registered: {Username}", registerDto.Username);
        return user;
    }

    public async Task<User?> FindUserByUsernameAsync(string username)
    {
        return await _unitOfWork.UserRepository.GetAll()
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<IEnumerable<UserSearchResultDto>> SearchUsersAsync(Guid currentUserId, string query, int limit)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query is required");

        var queryLower = query.ToLower();
        var contactSet = await _contactsService.GetContactIdsAsync(currentUserId);

        return await _unitOfWork.UserRepository.GetAll()
            .Where(u => u.Id != currentUserId && u.Username.ToLower().Contains(queryLower))
            .OrderBy(u => u.Username)
            .Take(limit)
            .Select(UserProjections.ToSearchResult(contactSet))
            .ToListAsync();
    }

    public async Task<Guid?> GetUserIdFromAuthHeader(string authHeader)
    {
        var username = _authService.GetUsernameFromToken(authHeader);

        var user = await _unitOfWork.UserRepository.GetAll()
            .Where(u => u.Username == username).FirstOrDefaultAsync();

        return user?.Id;
    }

    public async Task<UserProfileDto> GetUserProfileAsync(Guid userId)
    {
        return await _unitOfWork.UserRepository.GetAll()
            .Where(u => u.Id == userId)
            .Select(UserProjections.ToProfileDto)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("User not found");
    }

    public async Task<UserProfileDto> UpdateUserProfileAsync(Guid userId, UpdateProfileDto updateDto)
    {
        var user = await _unitOfWork.UserRepository.GetAsync(userId) ?? throw new InvalidOperationException("User not found");
        user.Email = updateDto.Email ?? user.Email;
        user.PhoneNumber = updateDto.PhoneNumber ?? user.PhoneNumber;
        user.FirstName = updateDto.FirstName ?? user.FirstName;
        user.LastName = updateDto.LastName ?? user.LastName;
        user.Bio = updateDto.Bio;

        await _unitOfWork.UserRepository.UpdateAsync(user);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("Profile updated for user {UserId}", userId);
        return await GetUserProfileAsync(userId);
    }
}

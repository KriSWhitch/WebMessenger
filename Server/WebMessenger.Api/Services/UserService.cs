using WebMessenger.DAL.Entities;
using WebMessenger.DAL.Interfaces;
using WebMessenger.Api.Models;
using Microsoft.EntityFrameworkCore;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Services.Interfaces;
using Dropbox.Api.Files;
using Dropbox.Api;

namespace WebMessenger.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;
    private readonly IContactsService _contactsService;
    private readonly IAuthService _authService;

    public UserService(IUnitOfWork unitOfWork, IConfiguration config, IContactsService contactsService, IAuthService authService)
    {
        _unitOfWork = unitOfWork;
        _config = config;
        _contactsService = contactsService;
        _authService = authService;
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

    public async Task<IEnumerable<UserSearchResultDto>> SearchUsersAsync(Guid currentUserId, string query, int limit)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query is required");

        var users = await SearchUsersAsync(query, limit);

        var results = users
            .Where(u => u.Id != currentUserId)
            .Select(u => new UserSearchResultDto
            {
                Id = u.Id,
                Username = u.Username,
                FirstName = u.FirstName,
                LastName = u.LastName,
                AvatarUrl = u.AvatarUrl,
                IsOnline = u.IsOnline,
                IsContact = _contactsService.IsContact(currentUserId, u.Id)
            });

        return results;
    }

    public async Task<Guid?> GetUserIdFromAuthHeader(string authHeader)
    {
        var username = _authService.GetUsernameFromToken(authHeader);

        var user = await _unitOfWork.UserRepository.GetAll()
            .Where(u => u.Username == username).FirstOrDefaultAsync();

        return user?.Id;
    }

    public UserProfileDto GetUserProfile(Guid userId)
    {
        var user = _unitOfWork.UserRepository.Get(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        return new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            IsOnline = user.IsOnline
        };
    }

    public async Task<UserProfileDto> UpdateUserProfileAsync(Guid userId, UpdateProfileDto updateDto)
    {
        var user = _unitOfWork.UserRepository.Get(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        user.Email = updateDto.Email ?? user.Email;
        user.PhoneNumber = updateDto.PhoneNumber ?? user.PhoneNumber;
        user.FirstName = updateDto.FirstName ?? user.FirstName;
        user.LastName = updateDto.LastName ?? user.LastName;
        user.Bio = updateDto.Bio;

        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.CommitAsync();

        return GetUserProfile(userId);
    }

    private async Task<IEnumerable<User>> SearchUsersAsync(string query, int limit = 10)
    {
        var queryLower = query.ToLower();
        return await _unitOfWork.UserRepository.GetAll()
            .Where(x => x.Username.ToLower().Contains(queryLower))
            .OrderBy(u => u.Username)
            .Take(limit)
            .ToListAsync();
    }
}
using WebMessenger.Api.Services.FileStorage;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.DAL.Interfaces;

namespace WebMessenger.Api.Services
{
    public class AvatarService : IAvatarService
    {
        private readonly IUnitOfWork _uow;
        private readonly IFileStorage _storage;
        private readonly ILogger<AvatarService> _logger;
        private const long MaxSize = 5 * 1024 * 1024;
        private static readonly string[] AllowedMime = { "image/jpeg", "image/png", "image/webp" };

        public AvatarService(IUnitOfWork uow, IFileStorage storage, ILogger<AvatarService> logger)
        {
            _uow = uow;
            _storage = storage;
            _logger = logger;
        }

        public async Task<string?> UpdateUserAvatarAsync(Guid userId, IFormFile file)
        {
            if (!_storage.ValidateFile(file, MaxSize, AllowedMime))
            {
                _logger.LogWarning("Invalid file upload attempt for user {UserId}", userId);
                return null;
            }

            var user = await _uow.UserRepository.GetAsync(userId);
            if (user == null) return null;

            try
            {
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                    await _storage.DeleteAsync(user.AvatarUrl);

                var uniqueName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                await using var stream = file.OpenReadStream();
                var newUrl = await _storage.UploadAsync(stream, uniqueName);
                if (newUrl == null) return null;

                user.AvatarUrl = newUrl;
                await _uow.CommitAsync();
                return newUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating avatar for user {UserId}", userId);
                return null;
            }
        }
    }
}

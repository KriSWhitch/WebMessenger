using WebMessenger.DAL.Interfaces;
using WebMessenger.Api.Services.Interfaces;
using Dropbox.Api.Files;
using Dropbox.Api;
using Dropbox.Api.Sharing;

namespace WebMessenger.Api.Services;

public class AvatarService : IAvatarService
{
    private const string DropboxFolder = "/avatars";
    private const string UserNotFoundTemplate = "User {UserId} not found";
    private const string UploadFailedTemplate = "Failed to upload new avatar";
    private const string UpdateAvatarErrorTemplate = "Error updating avatar for user {UserId}";
    private const string UploadAvatarErrorTemplate = "Error uploading avatar to Dropbox: {DropboxPath}";
    private const string DeleteSuccessTemplate = "Successfully deleted file: {FilePath}";
    private const string DeleteErrorTemplate = "Error deleting file. URL: {FileUrl}";

    private readonly IUnitOfWork _unitOfWork;
    private readonly DropboxClient _dropboxClient;
    private readonly ILogger<AvatarService> _logger;

    public AvatarService(
        ILogger<AvatarService> logger,
        IUnitOfWork unitOfWork,
        IConfiguration config)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var accessToken = config["Dropbox:AccessToken"]
            ?? throw new ArgumentException("Dropbox access token not configured");
        _dropboxClient = new DropboxClient(accessToken);
    }

    public async Task<string?> UpdateUserAvatarAsync(Guid userId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Attempt to upload empty file for user {UserId}", userId);
            return null;
        }

        var user = _unitOfWork.UserRepository.Get(userId);
        if (user == null)
        {
            _logger.LogWarning(UserNotFoundTemplate, userId);
            return null;
        }

        try
        {
            await DeleteOldAvatarIfExists(user.AvatarUrl);

            var newAvatarUrl = await UploadAvatarAsync(file, userId);
            if (newAvatarUrl == null)
            {
                _logger.LogError(UploadFailedTemplate);
                return null;
            }

            user.AvatarUrl = newAvatarUrl;
            await _unitOfWork.CommitAsync();

            return newAvatarUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, UpdateAvatarErrorTemplate, userId);
            return null;
        }
    }

    private async Task<string?> UploadAvatarAsync(IFormFile file, Guid userId)
    {
        var uniqueFileName = GenerateUniqueFileName(file, userId);
        var dropboxPath = $"{DropboxFolder}/{uniqueFileName}";

        try
        {
            await using var stream = file.OpenReadStream();
            await _dropboxClient.Files.UploadAsync(
                dropboxPath,
                WriteMode.Overwrite.Instance,
                body: stream);

            var sharedLink = await GetOrCreateSharedLink(dropboxPath);
            return ConvertToDirectLink(sharedLink.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, UploadAvatarErrorTemplate, dropboxPath);
            return null;
        }
    }

    private async Task<SharedLinkMetadata> GetOrCreateSharedLink(string dropboxPath)
    {
        try
        {
            return await _dropboxClient.Sharing.CreateSharedLinkWithSettingsAsync(dropboxPath);
        }
        catch (ApiException<CreateSharedLinkWithSettingsError> ex)
            when (ex.ErrorResponse.IsSharedLinkAlreadyExists)
        {
            var links = await _dropboxClient.Sharing.ListSharedLinksAsync(dropboxPath);
            return links.Links.First();
        }
    }

    private async Task DeleteOldAvatarIfExists(string? avatarUrl)
    {
        if (string.IsNullOrEmpty(avatarUrl)) return;

        try
        {
            var sharedLinkUrl = ConvertToSharedLink(avatarUrl);
            if (sharedLinkUrl == null) return;

            var linkMetadata = await _dropboxClient.Sharing.GetSharedLinkMetadataAsync(sharedLinkUrl);
            await _dropboxClient.Files.DeleteV2Async(linkMetadata.PathLower);

            _logger.LogInformation(DeleteSuccessTemplate, linkMetadata.PathLower);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, DeleteErrorTemplate, avatarUrl);
        }
    }

    private static string GenerateUniqueFileName(IFormFile file, Guid userId)
    {
        return $"{userId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
    }

    private static string ConvertToDirectLink(string sharedLink)
    {
        return sharedLink
            .Replace("www.dropbox.com", "dl.dropboxusercontent.com")
            .Replace("?dl=0", "");
    }

    private static string? ConvertToSharedLink(string directLink)
    {
        try
        {
            var uri = new Uri(directLink);

            if (uri.Host == "dl.dropboxusercontent.com")
            {
                var pathParts = uri.AbsolutePath.Split('/');
                if (pathParts.Length >= 4 && pathParts[1] == "scl" && pathParts[2] == "fi")
                {
                    var fileId = pathParts[3];
                    return $"https://www.dropbox.com/scl/fi/{fileId}/{string.Join("/", pathParts.Skip(4))}?dl=0";
                }
            }

            return directLink;
        }
        catch
        {
            return null;
        }
    }
}
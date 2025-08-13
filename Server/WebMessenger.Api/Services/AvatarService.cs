using WebMessenger.DAL.Entities;
using WebMessenger.DAL.Interfaces;
using WebMessenger.Api.Models;
using Microsoft.EntityFrameworkCore;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Services.Interfaces;
using Dropbox.Api.Files;
using Dropbox.Api;
using Dropbox.Api.Sharing;

namespace WebMessenger.Services;

public class AvatarService : IAvatarService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;
    private readonly DropboxClient _dropboxClient;
    private const string DropboxFolder = "/avatars";

    public AvatarService(IUnitOfWork unitOfWork, IConfiguration config)
    {
        _unitOfWork = unitOfWork;
        _config = config;

        var accessToken = _config["Dropbox:AccessToken"];
        _dropboxClient = new DropboxClient(accessToken);
    }

    public async Task<string?> UpdateUserAvatarAsync(Guid userId, IFormFile file)
    {
        try
        {
            var user = _unitOfWork.UserRepository.Get(userId);
            if (user == null)
            {
                // _logger.LogWarning($"User {userId} not found");
                return null;
            }

            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                await DeleteOldAvatar(user.AvatarUrl);
            }

            var newAvatarUrl = await UploadToDropbox(file, userId);
            if (newAvatarUrl == null)
            {
                // _logger.LogError("Failed to upload new avatar");
                return null;
            }

            user.AvatarUrl = newAvatarUrl;
            _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.CommitAsync();

            return newAvatarUrl;
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, $"Error updating avatar for user {userId}");
            return null;
        }
    }

    private async Task<string?> UploadToDropbox(IFormFile file, Guid userId)
    {
        var uniqueFileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var dropboxPath = $"{DropboxFolder}/{uniqueFileName}";

        try
        {
            using (var stream = file.OpenReadStream())
            {
                await _dropboxClient.Files.UploadAsync(
                    dropboxPath,
                    WriteMode.Overwrite.Instance,
                    body: stream);

                SharedLinkMetadata sharedLink;
                try
                {
                    sharedLink = await _dropboxClient.Sharing.CreateSharedLinkWithSettingsAsync(dropboxPath);
                }
                catch (ApiException<CreateSharedLinkWithSettingsError> ex)
                    when (ex.ErrorResponse.IsSharedLinkAlreadyExists)
                {
                    var links = await _dropboxClient.Sharing.ListSharedLinksAsync(dropboxPath);
                    sharedLink = links.Links.First();
                }

                return ConvertToDirectLink(sharedLink.Url);
            }
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, $"Error uploading avatar to Dropbox: {dropboxPath}");
            return null;
        }
    }

    private async Task DeleteOldAvatar(string avatarUrl)
    {
        if (string.IsNullOrEmpty(avatarUrl)) return;

        try
        {
            var sharedLinkUrl = ConvertToSharedLink(avatarUrl);
            if (sharedLinkUrl == null) return;

            var linkMetadata = await _dropboxClient.Sharing.GetSharedLinkMetadataAsync(sharedLinkUrl);

            await _dropboxClient.Files.DeleteV2Async(linkMetadata.PathLower);
            // _logger.LogInformation($"Successfully deleted file: {linkMetadata.PathLower}");
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, $"Error deleting file. URL: {avatarUrl}");
        }
    }

    private string ConvertToDirectLink(string sharedLink)
    {
        return sharedLink
            .Replace("www.dropbox.com", "dl.dropboxusercontent.com")
            .Replace("?dl=0", "");
    }

    private string? ConvertToSharedLink(string directLink)
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
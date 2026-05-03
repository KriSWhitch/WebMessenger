using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Sharing;
using Microsoft.Extensions.Options;
using WebMessenger.Api.Options;

namespace WebMessenger.Api.Services.FileStorage
{
    public class DropboxFileStorage : IFileStorage
    {
        private readonly DropboxClient _client;
        private readonly ILogger<DropboxFileStorage> _logger;
        private const string Folder = "/avatars";

        public DropboxFileStorage(IOptions<DropboxOptions> dropboxOptions, ILogger<DropboxFileStorage> logger)
        {
            _client = new DropboxClient(dropboxOptions.Value.AccessToken);
            _logger = logger;
        }

        public async Task<string?> UploadAsync(Stream fileStream, string fileName, CancellationToken ct = default)
        {
            var path = $"{Folder}/{fileName}";
            try
            {
                await _client.Files.UploadAsync(path, WriteMode.Overwrite.Instance, body: fileStream);
                var link = await GetOrCreateSharedLink(path);
                return ConvertToDirectLink(link.Url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file to Dropbox: {Path}", path);
                return null;
            }
        }

        public async Task<bool> DeleteAsync(string fileUrl, CancellationToken ct = default)
        {
            try
            {
                var sharedLink = ConvertToSharedLink(fileUrl);
                if (sharedLink == null) return false;
                var meta = await _client.Sharing.GetSharedLinkMetadataAsync(sharedLink);
                await _client.Files.DeleteV2Async(meta.PathLower);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file: {Url}", fileUrl);
                return false;
            }
        }

        public async Task<string?> GetDirectLinkAsync(string filePath, CancellationToken ct = default)
        {
            var link = await GetOrCreateSharedLink(filePath);
            return ConvertToDirectLink(link.Url);
        }

        public bool ValidateFile(IFormFile file, long maxSizeBytes, string[] allowedMimeTypes)
        {
            if (file == null || file.Length == 0) return false;
            if (file.Length > maxSizeBytes) return false;
            if (!allowedMimeTypes.Contains(file.ContentType)) return false;
            return true;
        }

        private async Task<SharedLinkMetadata> GetOrCreateSharedLink(string path)
        {
            try
            {
                return await _client.Sharing.CreateSharedLinkWithSettingsAsync(path);
            }
            catch (ApiException<CreateSharedLinkWithSettingsError> ex) when (ex.ErrorResponse.IsSharedLinkAlreadyExists)
            {
                var links = await _client.Sharing.ListSharedLinksAsync(path);
                return links.Links.First();
            }
        }

        private static string ConvertToDirectLink(string sharedLink)
            => sharedLink.Replace("www.dropbox.com", "dl.dropboxusercontent.com").Replace("?dl=0", "");

        private static string? ConvertToSharedLink(string directLink)
        {
            try
            {
                var uri = new Uri(directLink);
                if (uri.Host == "dl.dropboxusercontent.com")
                {
                    var parts = uri.AbsolutePath.Split('/');
                    if (parts.Length >= 4 && parts[1] == "scl" && parts[2] == "fi")
                    {
                        var fileId = parts[3];
                        return $"https://www.dropbox.com/scl/fi/{fileId}/{string.Join("/", parts.Skip(4))}?dl=0";
                    }
                }
                return directLink;
            }
            catch { return null; }
        }
    }
}

namespace WebMessenger.Api.Services.FileStorage
{
    public interface IFileStorage
    {
        Task<string?> UploadAsync(Stream fileStream, string fileName, CancellationToken ct = default);
        Task<bool> DeleteAsync(string fileUrl, CancellationToken ct = default);
        Task<string?> GetDirectLinkAsync(string filePath, CancellationToken ct = default);
        bool ValidateFile(IFormFile file, long maxSizeBytes, string[] allowedMimeTypes);
    }
}

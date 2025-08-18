namespace WebMessenger.Api.Services.Interfaces
{
    public interface IAvatarService
    {
        Task<string?> UpdateUserAvatarAsync(Guid value, IFormFile file);
    }
}

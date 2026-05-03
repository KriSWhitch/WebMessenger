using System.ComponentModel.DataAnnotations;

namespace WebMessenger.Api.Options
{
    public class DropboxOptions
    {
        public const string SectionName = "Dropbox";

        [Required]
        public required string AccessToken { get; init; }

        public string? ClientId { get; init; }
        public string? ClientSecret { get; init; }
    }
}

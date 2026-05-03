using System.ComponentModel.DataAnnotations;

namespace WebMessenger.Api.Options
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        [Required]
        [MinLength(32)]
        public required string Key { get; init; }

        [Required]
        public required string Issuer { get; init; }

        [Required]
        public required string Audience { get; init; }

        [Range(1, 365)]
        public int ExpireDays { get; init; } = 1;
    }
}

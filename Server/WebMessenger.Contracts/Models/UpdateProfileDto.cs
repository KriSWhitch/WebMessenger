using System.ComponentModel.DataAnnotations;

namespace WebMessenger.Contracts.Models
{
    public class UpdateProfileDto
    {
        [MaxLength(50)]
        public string? Username { get; set; }

        [EmailAddress]
        [MaxLength(255)]
        public string? Email { get; set; }

        [Phone]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [MaxLength(500)]
        public string? Bio { get; set; }

        [Url]
        [MaxLength(255)]
        public string? AvatarUrl { get; set; }
    }
}

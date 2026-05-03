using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WebMessenger.DAL.Entities
{
    [Index(nameof(Username), IsUnique = true)]
    [Index(nameof(Email))]
    [Index(nameof(IsOnline))]
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Username { get; set; }

        [Required]
        [MaxLength(255)]
        public required string PasswordHash { get; set; }

        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Bio { get; set; }

        [Url]
        [MaxLength(255)]
        public string? AvatarUrl { get; set; }

        public bool IsOnline { get; set; }
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        public virtual ICollection<Contact> Contacts { get; set; } = [];
        public virtual ICollection<ChatMember> ChatMemberships { get; set; } = [];
        public virtual ICollection<Message> Messages { get; set; } = [];
    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WebMessenger.DAL.Entities
{
    [Index(nameof(IsGroup))]
    public class Chat
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; }

        [Url]
        [MaxLength(255)]
        public string? AvatarUrl { get; set; }

        public bool IsGroup { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<ChatMember> Members { get; set; } = [];
        public virtual ICollection<Message> Messages { get; set; } = [];
    }
}

namespace WebMessenger.DAL.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public required string Username { get; set; }
        public required string PasswordHash { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();
        public virtual ICollection<ChatMember> ChatMemberships { get; set; } = new List<ChatMember>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}

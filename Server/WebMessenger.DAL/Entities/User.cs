namespace WebMessenger.DAL.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public required string Username { get; set; }
        public required string PasswordHash { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // public ICollection<User> Contacts { get; set; }
        // public ICollection<Chat> Chats { get; set; }
        // public ICollection<Message> Messages { get; set; }
    }
}

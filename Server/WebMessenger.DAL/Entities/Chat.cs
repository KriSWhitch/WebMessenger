namespace WebMessenger.DAL.Entities
{
    public class Chat
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsGroup { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<ChatMember> Members { get; set; } = [];
        public virtual ICollection<Message> Messages { get; set; } = [];
    }
}

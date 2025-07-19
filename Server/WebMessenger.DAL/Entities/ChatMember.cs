namespace WebMessenger.DAL.Entities
{
    public class ChatMember
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ChatId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastReadAt { get; set; }

        public virtual User? User { get; set; }
        public virtual Chat? Chat { get; set; }
    }
}

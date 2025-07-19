namespace WebMessenger.DAL.Entities
{
    public class Message
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid SenderId { get; set; }
        public Guid ChatId { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; set; }
        public bool IsRead { get; set; }

        public virtual User? Sender { get; set; }
        public virtual Chat? Chat { get; set; }
    }
}

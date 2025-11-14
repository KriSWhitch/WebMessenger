namespace WebMessenger.Api.Models
{
    public class ReadStateDto
    {
        public Guid ChatId { get; set; }
        public Guid UserId { get; set; }
        public DateTime LastReadAt { get; set; }
        public int UnreadCount { get; set; }
    }
}

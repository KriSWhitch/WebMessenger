namespace WebMessenger.Api.Models
{
    public class ChatMessagePreviewDto
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public string Snippet { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}

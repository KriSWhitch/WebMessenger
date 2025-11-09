namespace WebMessenger.Api.Models
{
    public class SendMessageResponse
    {
        public Guid ChatId { get; set; }
        public ChatMessageDto Message { get; set; } = new();
    }
}

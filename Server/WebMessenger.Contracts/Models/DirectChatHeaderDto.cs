namespace WebMessenger.Contracts.Models
{
    public class DirectChatHeaderDto
    {
        public Guid OtherUserId { get; set; }
        public string? Username { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
        public Guid? ChatId { get; set; }

    }
}

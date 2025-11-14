namespace WebMessenger.Api.Models
{
    public class ChatListItemDto
    {
        public Guid Id { get; set; }
        public bool IsGroup { get; set; }
        public string? Title { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime LastActivityAt { get; set; }
        public ChatMessagePreviewDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
        public bool HasUnread => UnreadCount > 0;
        public Guid? PeerUserId { get; set; }
        public string? PeerUsername { get; set; }
        public string? PeerAvatarUrl { get; set; }
    }
}
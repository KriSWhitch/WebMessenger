namespace WebMessenger.Api.Models
{
    public class ContactDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Nickname { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
        public DateTime AddedAt { get; set; }
        public Guid OwnerUserId { get; set; }
        public Guid ContactUserId { get; set; }
        public UserDto ContactUser { get; set; }
    }
}

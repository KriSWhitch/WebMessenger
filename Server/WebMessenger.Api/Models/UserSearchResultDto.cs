namespace WebMessenger.Api.Models
{
    public class UserSearchResultDto
    {
        public Guid Id { get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
        public bool IsContact { get; set; }
    }
}

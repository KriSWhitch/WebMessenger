namespace WebMessenger.Api.Models
{
    public class AddContactRequest
    {
        public Guid ContactUserId { get; set; }
        public string? Nickname { get; set; }
    }
}

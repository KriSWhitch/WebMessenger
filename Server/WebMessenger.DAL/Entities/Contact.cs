namespace WebMessenger.DAL.Entities
{
    public class Contact
    {
        public Guid Id { get; set; }
        public Guid OwnerUserId { get; set; }
        public Guid ContactUserId { get; set; }
        public string? Nickname { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public required virtual User OwnerUser { get; set; }
        public required virtual User ContactUser { get; set; }
    }
}

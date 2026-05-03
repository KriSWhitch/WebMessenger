using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebMessenger.DAL.Entities
{
    public class Contact
    {
        [Key]
        public Guid Id { get; set; }

        public Guid OwnerUserId { get; set; }
        public Guid ContactUserId { get; set; }

        [MaxLength(50)]
        public string? Nickname { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(OwnerUserId))]
        public required virtual User? OwnerUser { get; set; }

        [ForeignKey(nameof(ContactUserId))]
        public required virtual User? ContactUser { get; set; }
    }
}

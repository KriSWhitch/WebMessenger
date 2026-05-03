using System.ComponentModel.DataAnnotations;

namespace WebMessenger.Contracts.Models
{
    public class AddContactRequest
    {
        [Required]
        public Guid ContactUserId { get; set; }

        [MaxLength(50)]
        public string? Nickname { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace WebMessenger.Contracts.Models
{
    public class SendMessageRequest
    {
        [Required]
        [MaxLength(5000)]
        public required string Content { get; set; }
    }
}

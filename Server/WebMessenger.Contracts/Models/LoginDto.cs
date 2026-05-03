using System.ComponentModel.DataAnnotations;

namespace WebMessenger.Contracts.Models
{
    public class LoginDto
    {
        [Required]
        [MaxLength(50)]
        public required string Username { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Password { get; set; }
    }
}

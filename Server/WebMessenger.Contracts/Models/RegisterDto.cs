using System.ComponentModel.DataAnnotations;

namespace WebMessenger.Contracts.Models
{
    public class RegisterDto
    {
        [Required]
        [MaxLength(50)]
        public required string Username { get; set; }

        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public required string Password { get; set; }
    }
}

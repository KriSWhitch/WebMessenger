using System.ComponentModel.DataAnnotations;

namespace WebMessenger.Contracts.Models;

public sealed record LoginDto
{
    [Required]
    [MaxLength(50)]
    public required string Username { get; init; }

    [Required]
    [MaxLength(100)]
    public required string Password { get; init; }
}

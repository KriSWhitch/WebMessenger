using System.ComponentModel.DataAnnotations;

namespace WebMessenger.Contracts.Models;

public sealed record SendMessageRequest
{
    [Required]
    [MaxLength(5000)]
    public required string Content { get; init; }
}

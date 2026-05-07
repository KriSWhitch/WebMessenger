using System.ComponentModel.DataAnnotations;

namespace WebMessenger.Contracts.Models;

public sealed record AddContactRequest
{
    [Required]
    public Guid ContactUserId { get; init; }

    [MaxLength(50)]
    public string? Nickname { get; init; }
}

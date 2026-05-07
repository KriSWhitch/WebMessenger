using System.ComponentModel.DataAnnotations;

namespace WebMessenger.Contracts.Models;

public sealed record UpdateProfileDto
{
    [MaxLength(50)]
    public string? Username { get; init; }

    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; init; }

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; init; }

    [MaxLength(100)]
    public string? FirstName { get; init; }

    [MaxLength(100)]
    public string? LastName { get; init; }

    [MaxLength(500)]
    public string? Bio { get; init; }

    [Url]
    [MaxLength(255)]
    public string? AvatarUrl { get; init; }
}

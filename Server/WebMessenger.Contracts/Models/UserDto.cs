namespace WebMessenger.Contracts.Models;

public sealed record UserDto
{
    public Guid Id { get; init; }
    public required string Username { get; init; }
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public bool IsOnline { get; init; }
    public DateTime LastSeenAt { get; init; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; init; }
}

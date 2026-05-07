namespace WebMessenger.Contracts.Models;

public sealed record UserProfileDto
{
    public Guid Id { get; init; }
    public string? Username { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public bool IsOnline { get; init; }
}

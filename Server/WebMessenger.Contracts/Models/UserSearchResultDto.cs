namespace WebMessenger.Contracts.Models;

public sealed record UserSearchResultDto
{
    public Guid Id { get; init; }
    public string? Username { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? AvatarUrl { get; init; }
    public bool IsOnline { get; init; }
    public bool IsContact { get; init; }
}

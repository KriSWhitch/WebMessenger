namespace WebMessenger.Contracts.Models;

public sealed record ContactDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string? Nickname { get; init; }
    public string? Username { get; init; }
    public string? AvatarUrl { get; init; }
    public bool IsOnline { get; init; }
    public DateTime AddedAt { get; init; }
    public Guid OwnerUserId { get; init; }
    public Guid ContactUserId { get; init; }
    public UserDto? ContactUser { get; init; }
}

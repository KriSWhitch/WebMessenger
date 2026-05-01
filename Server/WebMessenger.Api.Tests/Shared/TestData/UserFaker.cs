using Bogus;
using WebMessenger.DAL.Entities;

namespace WebMessenger.Api.Tests.Shared.TestData;

/// <summary>
/// Deterministic <see cref="User"/> builder backed by Bogus.
/// Uses per-instance seeding via <c>UseSeed()</c> so that parallel tests do not
/// interfere with each other through the global <c>Randomizer.Seed</c>.
/// </summary>
public static class UserFaker
{
    public static Faker<User> Create(int seed = 1337) =>
        new Faker<User>()
            .UseSeed(seed)
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.Username, f => f.Internet.UserName())
            .RuleFor(u => u.PasswordHash, f => BCrypt.Net.BCrypt.HashPassword(f.Internet.Password()))
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber())
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.Bio, f => f.Lorem.Sentence())
            .RuleFor(u => u.AvatarUrl, _ => null)
            .RuleFor(u => u.IsOnline, _ => false)
            .RuleFor(u => u.LastSeenAt, f => f.Date.Past().ToUniversalTime())
            .RuleFor(u => u.CreatedAt, f => f.Date.Past().ToUniversalTime())
            .RuleFor(u => u.LastLoginAt, _ => null);

    public static User Single(int seed = 1337) => Create(seed).Generate();
    public static List<User> Many(int count, int seed = 1337) => Create(seed).Generate(count);
}

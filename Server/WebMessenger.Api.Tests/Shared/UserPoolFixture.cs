using WebMessenger.Api.Tests.Shared.TestData;
using WebMessenger.DAL.Entities;

namespace WebMessenger.Api.Tests.Shared;

/// <summary>
/// Shared class fixture providing a pre-built deterministic <see cref="User"/> pool.
/// Use as xUnit class fixture: implement <c>IClassFixture<UserPoolFixture></c>.
/// </summary>
public sealed class UserPoolFixture
{
    /// <summary>Fixed set of 10 deterministic users available across all test methods in a class.</summary>
    public IReadOnlyList<User> Users { get; } = UserFaker.Many(10, seed: 42);
}

/// <summary>
/// Collection fixture marker — share a single <see cref="UserPoolFixture"/> across multiple test classes.
/// </summary>
[CollectionDefinition(Name)]
public sealed class UserPoolCollection : ICollectionFixture<UserPoolFixture>
{
    public const string Name = "UserPool";
}

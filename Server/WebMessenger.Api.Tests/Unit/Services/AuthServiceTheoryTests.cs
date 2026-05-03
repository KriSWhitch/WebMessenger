using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WebMessenger.Api.Options;
using WebMessenger.Api.Services;
using WebMessenger.Api.Tests.Shared;
using WebMessenger.DAL.Entities;

namespace WebMessenger.Api.Tests.Unit.Services;

/// <summary>
/// Theory + class-fixture tests for <see cref="AuthService"/>.
/// Demonstrates:
///  - [Theory] with boundary / invalid inputs
///  - <see cref="IClassFixture{T}"/> sharing a pre-built user pool across test methods
///  - <see cref="UserPoolFixture"/> providing deterministic data
/// </summary>
public class AuthServiceTheoryTests : IClassFixture<UserPoolFixture>
{
    private readonly AuthService _sut;
    private readonly IReadOnlyList<User> _users;

    public AuthServiceTheoryTests(UserPoolFixture fixture)
    {
        _users = fixture.Users;

        var jwtOptions = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Key      = "super-secret-test-key-that-is-long-enough-32chars",
            Issuer   = "test-issuer",
            Audience = "test-audience"
        });
        _sut = new AuthService(jwtOptions, NullLogger<AuthService>.Instance);
    }

    // -------------------------------------------------------------------------
    // Boundary: password edge cases
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void ValidateUserCredentials_BlankOrWhitespacePassword_ReturnsFalse(string password)
    {
        // Arrange — take the first pooled user
        var user = _users[0];

        // Act
        var result = _sut.ValidateUserCredentials(user, password);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("a")]           // 1-char password — should still be validated against hash
    [InlineData("12345678901234567890123456789012")] // 32-char password
    public void ValidateUserCredentials_NonBlankWrongPasswords_ReturnsFalse(string password)
    {
        // Arrange — password hash was generated with a different value
        var user = _users[1];

        // Act
        var result = _sut.ValidateUserCredentials(user, password);

        // Assert
        Assert.False(result);
    }

    // -------------------------------------------------------------------------
    // Theory: JWT token contains expected number of segments (header.payload.sig)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void GenerateJwtToken_VariousPooledUsers_AlwaysReturnsThreePartToken(int userIndex)
    {
        // Arrange
        var user = _users[userIndex];

        // Act
        var token = _sut.GenerateJwtToken(user);

        // Assert
        Assert.Equal(3, token.Split('.').Length);
    }

    // -------------------------------------------------------------------------
    // Theory: GetUsernameFromToken round-trip for multiple users
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(7)]
    public void GetUsernameFromToken_TokenGeneratedForPooledUser_ReturnsCorrectUsername(int userIndex)
    {
        // Arrange
        var user  = _users[userIndex];
        var token = _sut.GenerateJwtToken(user);

        // Act
        var username = _sut.GetUsernameFromToken($"Bearer {token}");

        // Assert
        Assert.Equal(user.Username, username);
    }
}

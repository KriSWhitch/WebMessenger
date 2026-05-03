using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using WebMessenger.Api.Options;
using WebMessenger.Api.Services;
using WebMessenger.Api.Tests.Shared.TestData;
using WebMessenger.DAL.Entities;

namespace WebMessenger.Api.Tests.Unit.Services;

/// <summary>
/// Unit tests for <see cref="AuthService"/>.
/// Covers: credential validation (positive / negative / boundary) and JWT token generation.
/// </summary>
public class AuthServiceTests
{
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var jwtOptions = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Key      = "super-secret-test-key-that-is-long-enough-32chars",
            Issuer   = "test-issuer",
            Audience = "test-audience"
        });

        _sut = new AuthService(jwtOptions, NullLogger<AuthService>.Instance);
    }

    // -------------------------------------------------------------------------
    // ValidateUserCredentials
    // -------------------------------------------------------------------------

    [Fact]
    public void ValidateUserCredentials_ValidPassword_ReturnsTrue()
    {
        // Arrange
        const string rawPassword = "P@ssw0rd!";
        var user = UserFaker.Single();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword);

        // Act
        var result = _sut.ValidateUserCredentials(user, rawPassword);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateUserCredentials_WrongPassword_ReturnsFalse()
    {
        // Arrange
        var user = UserFaker.Single();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password");

        // Act
        var result = _sut.ValidateUserCredentials(user, "wrong-password");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateUserCredentials_NullUser_ReturnsFalse()
    {
        // Arrange / Act
        var result = _sut.ValidateUserCredentials(null, "any-password");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUserCredentials_EmptyOrWhitespacePassword_ReturnsFalse(string password)
    {
        // Arrange
        var user = UserFaker.Single();

        // Act
        var result = _sut.ValidateUserCredentials(user, password);

        // Assert
        Assert.False(result);
    }

    // -------------------------------------------------------------------------
    // GenerateJwtToken
    // -------------------------------------------------------------------------

    [Fact]
    public void GenerateJwtToken_ValidUser_ReturnsNonEmptyToken()
    {
        // Arrange
        var user = UserFaker.Single();

        // Act
        var token = _sut.GenerateJwtToken(user);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(3, token.Split('.').Length); // header.payload.signature
    }

    [Fact]
    public void GenerateJwtToken_NullUser_ThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => _sut.GenerateJwtToken(null!));
    }

    // -------------------------------------------------------------------------
    // GetUsernameFromToken
    // -------------------------------------------------------------------------

    [Fact]
    public void GetUsernameFromToken_ValidBearerToken_ReturnsUsername()
    {
        // Arrange
        var user = UserFaker.Single();
        var token = _sut.GenerateJwtToken(user);
        var authHeader = $"Bearer {token}";

        // Act
        var username = _sut.GetUsernameFromToken(authHeader);

        // Assert
        Assert.Equal(user.Username, username);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("InvalidTokenWithNoBearer")]
    public void GetUsernameFromToken_InvalidInput_ReturnsNull(string? authHeader)
    {
        // Arrange / Act
        var result = _sut.GetUsernameFromToken(authHeader!);

        // Assert
        Assert.Null(result);
    }
}

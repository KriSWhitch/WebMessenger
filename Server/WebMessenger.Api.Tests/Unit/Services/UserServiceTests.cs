using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebMessenger.Api.Services;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Api.Tests.Shared;
using WebMessenger.Api.Tests.Shared.Mocks;
using WebMessenger.Api.Tests.Shared.TestData;
using WebMessenger.Contracts.Models;
using WebMessenger.DAL.Entities;
using WebMessenger.DAL.Interfaces;

namespace WebMessenger.Api.Tests.Unit.Services;

/// <summary>
/// Unit tests for <see cref="UserService"/>.
/// Demonstrates: Moq setup, configured returns, configured throws, Verify interaction checks.
/// </summary>
public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IContactsService> _contactsMock;
    private readonly Mock<IAuthService> _authMock;
    private readonly Mock<IRepository<User>> _userRepoMock;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _uowMock       = UnitOfWorkMockHelper.Create();
        _contactsMock  = new Mock<IContactsService>();
        _authMock      = new Mock<IAuthService>();
        _userRepoMock  = new Mock<IRepository<User>>();

        _uowMock.Setup(u => u.UserRepository).Returns(_userRepoMock.Object);

        _sut = new UserService(_uowMock.Object, _contactsMock.Object, _authMock.Object, NullLogger<UserService>.Instance);
    }

    // -------------------------------------------------------------------------
    // IsUsernameExistsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task IsUsernameExistsAsync_ExistingUsername_ReturnsTrue()
    {
        // Arrange
        var user = UserFaker.Single();
        _userRepoMock.Setup(r => r.GetAll(It.IsAny<string[]>()))
            .Returns(new[] { user }.AsAsyncQueryable());

        // Act
        var result = await _sut.IsUsernameExistsAsync(user.Username);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsUsernameExistsAsync_NonExistingUsername_ReturnsFalse()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetAll(It.IsAny<string[]>()))
            .Returns(Enumerable.Empty<User>().AsAsyncQueryable());

        // Act
        var result = await _sut.IsUsernameExistsAsync("ghost-user");

        // Assert
        Assert.False(result);
    }

    // -------------------------------------------------------------------------
    // RegisterUserAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RegisterUserAsync_ValidDto_InsertsUserAndCommits()
    {
        // Arrange
        var dto = new RegisterDto { Username = "newuser", Password = "P@ssw0rd!" };
        _userRepoMock.Setup(r => r.InsertAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var user = await _sut.RegisterUserAsync(dto);

        // Assert
        Assert.Equal(dto.Username, user.Username);
        _userRepoMock.Verify(r => r.InsertAsync(It.Is<User>(u => u.Username == dto.Username), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // FindUserByUsernameAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task FindUserByUsernameAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = UserFaker.Single();
        _userRepoMock.Setup(r => r.GetAll(It.IsAny<string[]>()))
            .Returns(new[] { user }.AsAsyncQueryable());

        // Act
        var result = await _sut.FindUserByUsernameAsync(user.Username);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Username, result.Username);
    }

    [Fact]
    public async Task FindUserByUsernameAsync_NonExistingUser_ReturnsNull()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetAll(It.IsAny<string[]>()))
            .Returns(Enumerable.Empty<User>().AsAsyncQueryable());

        // Act
        var result = await _sut.FindUserByUsernameAsync("nobody");

        // Assert
        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // SearchUsersAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SearchUsersAsync_BlankQuery_ThrowsArgumentException()
    {
        // Arrange / Act / Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.SearchUsersAsync(Guid.NewGuid(), "   ", 10));
    }

    [Fact]
    public async Task SearchUsersAsync_ValidQuery_ExcludesCurrentUser()
    {
        // Arrange
        var me = UserFaker.Single(seed: 1);
        var other = UserFaker.Single(seed: 2);
        _userRepoMock.Setup(r => r.GetAll(It.IsAny<string[]>()))
            .Returns(new[] { me, other }.AsAsyncQueryable());
        _contactsMock.Setup(c => c.IsContact(me.Id, other.Id)).Returns(false);

        // Act
        var results = (await _sut.SearchUsersAsync(me.Id, other.Username, 10)).ToList();

        // Assert — me must not appear in results
        Assert.DoesNotContain(results, r => r.Id == me.Id);
    }

    // -------------------------------------------------------------------------
    // GetUserProfileAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUserProfileAsync_ExistingUser_ReturnsProfileDto()
    {
        // Arrange
        var user = UserFaker.Single();
        _userRepoMock.Setup(r => r.GetAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var profile = await _sut.GetUserProfileAsync(user.Id);

        // Assert
        Assert.Equal(user.Id, profile.Id);
        Assert.Equal(user.Username, profile.Username);
    }

    [Fact]
    public async Task GetUserProfileAsync_UnknownUser_ThrowsInvalidOperationException()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GetUserProfileAsync(Guid.NewGuid()));
    }

    // -------------------------------------------------------------------------
    // UpdateUserProfileAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateUserProfileAsync_ValidDto_UpdatesFieldsAndCommits()
    {
        // Arrange
        var user = UserFaker.Single();
        _userRepoMock.Setup(r => r.GetAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new UpdateProfileDto { FirstName = "Updated", Bio = "new bio" };

        // Act
        var profile = await _sut.UpdateUserProfileAsync(user.Id, dto);

        // Assert
        Assert.Equal("Updated", profile.FirstName);
        Assert.Equal("new bio", profile.Bio);
        _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

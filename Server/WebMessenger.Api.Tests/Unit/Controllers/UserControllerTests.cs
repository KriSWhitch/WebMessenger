using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebMessenger.Api.Controllers;
using WebMessenger.Api.Infrastructure.Interfaces;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Api.Tests.Shared.TestData;
using WebMessenger.Contracts.Models;

namespace WebMessenger.Api.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for <see cref="UserController"/>.
/// </summary>
public class UserControllerTests
{
    private readonly Mock<IUserService>    _userMock;
    private readonly Mock<IAvatarService>  _avatarMock;
    private readonly Mock<ICurrentUser>    _currentUserMock;
    private readonly UserController        _sut;
    private readonly Guid                  _userId = Guid.NewGuid();

    public UserControllerTests()
    {
        _userMock        = new Mock<IUserService>();
        _avatarMock      = new Mock<IAvatarService>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(c => c.Id).Returns(_userId);

        _sut = new UserController(
            NullLogger<UserController>.Instance,
            _userMock.Object,
            _avatarMock.Object,
            _currentUserMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // -------------------------------------------------------------------------
    // Index (GET /api/users?query=...)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Index_EmptyQuery_ReturnsBadRequest()
    {
        // Arrange / Act
        var result = await _sut.Index(query: "");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Index_ValidQuery_ReturnsOkWithResults()
    {
        // Arrange
        var dto = new UserSearchResultDto { Id = Guid.NewGuid(), Username = "bob" };
        _userMock.Setup(s => s.SearchUsersAsync(_userId, "bob", 10))
            .ReturnsAsync([dto]);

        // Act
        var result = await _sut.Index(query: "bob", limit: 10);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    // -------------------------------------------------------------------------
    // GetUserProfile (GET /api/users/profile)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUserProfile_ReturnsOkWithProfile()
    {
        // Arrange
        var profile = new UserProfileDto { Id = _userId, Username = "me" };
        _userMock.Setup(s => s.GetUserProfileAsync(_userId)).ReturnsAsync(profile);

        // Act
        var result = await _sut.GetUserProfile();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(profile, ok.Value);
    }

    // -------------------------------------------------------------------------
    // UpdateUserProfile (PUT /api/users/profile)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateUserProfile_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto     = new UpdateProfileDto { FirstName = "John" };
        var profile = new UserProfileDto { Id = _userId, FirstName = "John" };
        _userMock.Setup(s => s.UpdateUserProfileAsync(_userId, dto)).ReturnsAsync(profile);

        // Act
        var result = await _sut.UpdateUserProfile(dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(profile, ok.Value);
    }

    // -------------------------------------------------------------------------
    // UploadAvatar (POST /api/users/avatar)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UploadAvatar_NoFile_ReturnsBadRequest()
    {
        // Arrange / Act
        var result = await _sut.UploadAvatar(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadAvatar_NonImageFile_ReturnsBadRequest()
    {
        // Arrange
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(1024);
        file.Setup(f => f.ContentType).Returns("application/pdf");

        // Act
        var result = await _sut.UploadAvatar(file.Object);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadAvatar_FileTooLarge_ReturnsBadRequest()
    {
        // Arrange
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6 MB
        file.Setup(f => f.ContentType).Returns("image/jpeg");

        // Act
        var result = await _sut.UploadAvatar(file.Object);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadAvatar_AvatarServiceReturnsNull_Returns500()
    {
        // Arrange
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(512);
        file.Setup(f => f.ContentType).Returns("image/jpeg");
        _avatarMock.Setup(s => s.UpdateUserAvatarAsync(_userId, file.Object))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _sut.UploadAvatar(file.Object);

        // Assert
        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task UploadAvatar_Success_ReturnsOkWithUrl()
    {
        // Arrange
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(512);
        file.Setup(f => f.ContentType).Returns("image/jpeg");
        _avatarMock.Setup(s => s.UpdateUserAvatarAsync(_userId, file.Object))
            .ReturnsAsync("https://cdn.example.com/avatar.jpg");

        // Act
        var result = await _sut.UploadAvatar(file.Object);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebMessenger.Api.Controllers;
using WebMessenger.Api.Infrastructure.Interfaces;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Api.Tests.Shared.TestData;
using WebMessenger.Contracts.Models;

namespace WebMessenger.Api.Tests.Unit.ErrorHandling;

/// <summary>
/// Tests covering error-path HTTP status codes returned by controllers.
/// Demonstrates: 400/401/404/500-style paths in a single fixture.
/// </summary>
public class ControllerErrorHandlingTests
{
    // ---- Auth ---------------------------------------------------------------

    [Fact]
    public async Task AuthController_Login_UserNotFound_Returns401()
    {
        // Arrange
        var userMock = new Mock<IUserService>();
        userMock.Setup(s => s.FindUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((DAL.Entities.User?)null);
        var sut = BuildAuthController(userMock.Object);

        // Act
        var result = await sut.Login(new LoginDto { Username = "x", Password = "y" });

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task AuthController_Register_DuplicateUsername_Returns400()
    {
        // Arrange
        var userMock = new Mock<IUserService>();
        userMock.Setup(s => s.IsUsernameExistsAsync("taken")).ReturnsAsync(true);
        var sut = BuildAuthController(userMock.Object);

        // Act
        var result = await sut.Register(new RegisterDto { Username = "taken", Password = "P@ss1" });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ---- User ---------------------------------------------------------------

    [Fact]
    public async Task UserController_Index_EmptyQuery_Returns400()
    {
        // Arrange
        var sut = BuildUserController(new Mock<IUserService>().Object);

        // Act
        var result = await sut.Index(query: "");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UserController_UploadAvatar_NoFile_Returns400()
    {
        // Arrange
        var sut = BuildUserController(new Mock<IUserService>().Object);

        // Act
        var result = await sut.UploadAvatar(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UserController_UploadAvatar_ServiceFails_Returns500()
    {
        // Arrange
        var avatarMock = new Mock<IAvatarService>();
        avatarMock.Setup(s => s.UpdateUserAvatarAsync(It.IsAny<Guid>(), It.IsAny<IFormFile>()))
            .ReturnsAsync((string?)null);
        var sut = BuildUserController(new Mock<IUserService>().Object, avatarMock.Object);

        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(512);
        file.Setup(f => f.ContentType).Returns("image/jpeg");

        // Act
        var result = await sut.UploadAvatar(file.Object);

        // Assert
        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, obj.StatusCode);
    }

    // ---- Contact ------------------------------------------------------------

    [Fact]
    public async Task ContactController_AddContact_SelfAdd_Returns400()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sut    = BuildContactController(userId);

        // Act
        var result = await sut.AddContact(new AddContactRequest { ContactUserId = userId });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ---- Helpers ------------------------------------------------------------

    private static AuthController BuildAuthController(IUserService userService)
    {
        var sut = new AuthController(new Mock<IAuthService>().Object, userService, NullLogger<AuthController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return sut;
    }

    private static UserController BuildUserController(IUserService userService, IAvatarService? avatarService = null)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(c => c.Id).Returns(Guid.NewGuid());
        var sut = new UserController(
            NullLogger<UserController>.Instance,
            userService,
            avatarService ?? new Mock<IAvatarService>().Object,
            currentUser.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return sut;
    }

    private static ContactController BuildContactController(Guid userId)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(c => c.Id).Returns(userId);
        var contacts = new Mock<IContactsService>();
        var sut = new ContactController(NullLogger<ContactController>.Instance, contacts.Object, currentUser.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return sut;
    }
}

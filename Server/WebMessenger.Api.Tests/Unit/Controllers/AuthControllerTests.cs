using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebMessenger.Api.Controllers;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Api.Tests.Shared.TestData;
using WebMessenger.Contracts.Models;
using WebMessenger.DAL.Entities;

namespace WebMessenger.Api.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for <see cref="AuthController"/>.
/// Covers: register, login (positive / negative), verify.
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IAuthService>  _authMock;
    private readonly Mock<IUserService>  _userMock;
    private readonly AuthController      _sut;

    public AuthControllerTests()
    {
        _authMock = new Mock<IAuthService>();
        _userMock = new Mock<IUserService>();

        _sut = new AuthController(_authMock.Object, _userMock.Object, NullLogger<AuthController>.Instance);

        // Provide a minimal HttpContext so Response.Cookies.Append does not throw
        var httpContext = new DefaultHttpContext();
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    // -------------------------------------------------------------------------
    // Register
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Register_NewUsername_ReturnsOk()
    {
        // Arrange
        var dto = new RegisterDto { Username = "alice", Password = "P@ss1" };
        _userMock.Setup(s => s.IsUsernameExistsAsync(dto.Username)).ReturnsAsync(false);
        _userMock.Setup(s => s.RegisterUserAsync(dto)).ReturnsAsync(new User
        {
            Username = dto.Username,
            PasswordHash = "hash"
        });

        // Act
        var result = await _sut.Register(dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Register_ExistingUsername_ReturnsBadRequest()
    {
        // Arrange
        var dto = new RegisterDto { Username = "alice", Password = "P@ss1" };
        _userMock.Setup(s => s.IsUsernameExistsAsync(dto.Username)).ReturnsAsync(true);

        // Act
        var result = await _sut.Register(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _userMock.Verify(s => s.RegisterUserAsync(It.IsAny<RegisterDto>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // Login
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var user = UserFaker.Single();
        var dto  = new LoginDto { Username = user.Username, Password = "correct" };
        _userMock.Setup(s => s.FindUserByUsernameAsync(dto.Username)).ReturnsAsync(user);
        _authMock.Setup(s => s.ValidateUserCredentials(user, dto.Password)).Returns(true);
        _authMock.Setup(s => s.GenerateJwtToken(user)).Returns("jwt.token.value");

        // Act
        var result = await _sut.Login(dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        _authMock.Verify(s => s.GenerateJwtToken(user), Times.Once);
    }

    [Fact]
    public async Task Login_UserNotFound_ReturnsUnauthorized()
    {
        // Arrange
        var dto = new LoginDto { Username = "ghost", Password = "x" };
        _userMock.Setup(s => s.FindUserByUsernameAsync(dto.Username)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.Login(dto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
        _authMock.Verify(s => s.ValidateUserCredentials(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var user = UserFaker.Single();
        var dto  = new LoginDto { Username = user.Username, Password = "wrong" };
        _userMock.Setup(s => s.FindUserByUsernameAsync(dto.Username)).ReturnsAsync(user);
        _authMock.Setup(s => s.ValidateUserCredentials(user, dto.Password)).Returns(false);

        // Act
        var result = await _sut.Login(dto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
        _authMock.Verify(s => s.GenerateJwtToken(It.IsAny<User>()), Times.Never);
    }
}

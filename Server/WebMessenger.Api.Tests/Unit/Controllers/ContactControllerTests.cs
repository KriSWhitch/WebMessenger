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
/// Unit tests for <see cref="ContactController"/>.
/// </summary>
public class ContactControllerTests
{
    private readonly Mock<IContactsService> _contactsMock;
    private readonly Mock<ICurrentUser>     _currentUserMock;
    private readonly ContactController      _sut;
    private readonly Guid                   _userId = Guid.NewGuid();

    public ContactControllerTests()
    {
        _contactsMock    = new Mock<IContactsService>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(c => c.Id).Returns(_userId);

        _sut = new ContactController(
            NullLogger<ContactController>.Instance,
            _contactsMock.Object,
            _currentUserMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // -------------------------------------------------------------------------
    // Index (GET /api/contacts)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Index_ReturnsOkWithContactList()
    {
        // Arrange
        var user = UserFaker.Single();
        var contacts = new List<ContactDto>
        {
            new() { Id = Guid.NewGuid(), UserId = user.Id, Nickname = user.Username }
        };
        _contactsMock.Setup(s => s.GetContactsAsync(_userId, "")).ReturnsAsync(contacts);

        // Act
        var result = await _sut.Index();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<IEnumerable<ContactDto>>(ok.Value, exactMatch: false);
        Assert.Single(list);
    }

    // -------------------------------------------------------------------------
    // AddContact (POST /api/contacts/add)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddContact_SelfAdd_ReturnsBadRequest()
    {
        // Arrange
        var request = new AddContactRequest { ContactUserId = _userId };

        // Act
        var result = await _sut.AddContact(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _contactsMock.Verify(s => s.AddContactAsync(It.IsAny<Guid>(), It.IsAny<AddContactRequest>()), Times.Never);
    }

    [Fact]
    public async Task AddContact_AlreadyExists_ReturnsBadRequest()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var request = new AddContactRequest { ContactUserId = otherUserId };
        _contactsMock.Setup(s => s.IsContactAsync(_userId, otherUserId)).ReturnsAsync(true);

        // Act
        var result = await _sut.AddContact(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AddContact_ValidNewContact_ReturnsOk()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var request  = new AddContactRequest { ContactUserId = otherUserId };
        var response = new AddContactResponse { ContactId = Guid.NewGuid() };
        _contactsMock.Setup(s => s.IsContactAsync(_userId, otherUserId)).ReturnsAsync(false);
        _contactsMock.Setup(s => s.AddContactAsync(_userId, request)).ReturnsAsync(response);

        // Act
        var result = await _sut.AddContact(request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
        _contactsMock.Verify(s => s.AddContactAsync(_userId, request), Times.Once);
    }
}

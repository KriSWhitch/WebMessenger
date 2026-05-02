using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebMessenger.Api.Services;
using WebMessenger.Api.Tests.Shared;
using WebMessenger.Api.Tests.Shared.Mocks;
using WebMessenger.Api.Tests.Shared.TestData;
using WebMessenger.Contracts.Models;
using WebMessenger.DAL.Entities;
using WebMessenger.DAL.Interfaces;

namespace WebMessenger.Api.Tests.Unit.Services;

/// <summary>
/// Unit tests for <see cref="ContactsService"/>.
/// </summary>
public class ContactsServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IRepository<Contact>> _contactRepoMock;
    private readonly Mock<IRepository<User>> _userRepoMock;
    private readonly ContactsService _sut;

    public ContactsServiceTests()
    {
        _uowMock           = UnitOfWorkMockHelper.Create();
        _contactRepoMock   = new Mock<IRepository<Contact>>();
        _userRepoMock      = new Mock<IRepository<User>>();

        _uowMock.Setup(u => u.ContactRepository).Returns(_contactRepoMock.Object);
        _uowMock.Setup(u => u.UserRepository).Returns(_userRepoMock.Object);

        _sut = new ContactsService(_uowMock.Object, NullLogger<ContactsService>.Instance);
    }

    // -------------------------------------------------------------------------
    // AddContactAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddContactAsync_SelfAdd_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AddContactRequest { ContactUserId = userId };

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AddContactAsync(userId, request));
    }

    [Fact]
    public async Task AddContactAsync_AlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var owner = UserFaker.Single(seed: 1);
        var contact = UserFaker.Single(seed: 2);

        // Existing contact record
        var existingContact = new Contact
        {
            OwnerUserId = owner.Id,
            ContactUserId = contact.Id,
            AddedAt = DateTime.UtcNow,
            OwnerUser = owner,
            ContactUser = contact
        };
        _contactRepoMock.Setup(r => r.GetAll(It.IsAny<string[]>()))
            .Returns(new[] { existingContact }.AsAsyncQueryable());

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AddContactAsync(owner.Id, new AddContactRequest { ContactUserId = contact.Id }));
    }

    [Fact]
    public async Task AddContactAsync_NewContact_InsertsAndCommits()
    {
        // Arrange
        var owner   = UserFaker.Single(seed: 1);
        var contact = UserFaker.Single(seed: 2);

        _contactRepoMock.Setup(r => r.GetAll(It.IsAny<string[]>()))
            .Returns(Enumerable.Empty<Contact>().AsAsyncQueryable());
        _userRepoMock.Setup(r => r.GetAsync(owner.Id,   It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _userRepoMock.Setup(r => r.GetAsync(contact.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contact);
        _contactRepoMock.Setup(r => r.InsertAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _sut.AddContactAsync(owner.Id, new AddContactRequest { ContactUserId = contact.Id });

        // Assert
        // ContactId is assigned by EF on save — in the unit test the Id will be Guid.Empty
        // (no real DB), so we verify the insert and commit were called instead.
        _contactRepoMock.Verify(r => r.InsertAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // IsContact / IsContactAsync
    // -------------------------------------------------------------------------

    [Fact]
    public void IsContact_ExistingRelationship_ReturnsTrue()
    {
        // Arrange
        var owner   = Guid.NewGuid();
        var contact = Guid.NewGuid();
        var ownerUser   = UserFaker.Single(seed: 10);
        var contactUser = UserFaker.Single(seed: 11);
        ownerUser.Id   = owner;
        contactUser.Id = contact;
        var record = new Contact { OwnerUserId = owner, ContactUserId = contact, AddedAt = DateTime.UtcNow, OwnerUser = ownerUser, ContactUser = contactUser };
        _contactRepoMock.Setup(r => r.GetAll(It.IsAny<string[]>()))
            .Returns(new[] { record }.AsQueryable());

        // Act
        var result = _sut.IsContact(owner, contact);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsContact_NoRelationship_ReturnsFalse()
    {
        // Arrange
        _contactRepoMock.Setup(r => r.GetAll(It.IsAny<string[]>()))
            .Returns(Enumerable.Empty<Contact>().AsQueryable());

        // Act
        var result = _sut.IsContact(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    // -------------------------------------------------------------------------
    // GetContactsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetContactsAsync_NoContacts_ReturnsEmptyList()
    {
        // Arrange
        _contactRepoMock.Setup(r => r.GetAll(It.IsAny<string[]>()))
            .Returns(Enumerable.Empty<Contact>().AsAsyncQueryable());

        // Act
        var results = (await _sut.GetContactsAsync(Guid.NewGuid(), "")).ToList();

        // Assert
        Assert.Empty(results);
    }
}

using Moq;
using WebMessenger.DAL.Entities;
using WebMessenger.DAL.Interfaces;

namespace WebMessenger.Api.Tests.Shared.Mocks;

/// <summary>
/// Reusable helpers that configure common <see cref="IUnitOfWork"/> mock seams.
/// </summary>
public static class UnitOfWorkMockHelper
{
    /// <summary>
    /// Creates a fully configured <see cref="Mock{IUnitOfWork}"/> with empty repository stubs.
    /// Individual tests can override specific setups on top of this baseline.
    /// </summary>
    public static Mock<IUnitOfWork> Create()
    {
        var mock = new Mock<IUnitOfWork>();

        mock.Setup(u => u.UserRepository).Returns(new Mock<IRepository<User>>().Object);
        mock.Setup(u => u.ChatRepository).Returns(new Mock<IRepository<Chat>>().Object);
        mock.Setup(u => u.ChatMemberRepository).Returns(new Mock<IRepository<ChatMember>>().Object);
        mock.Setup(u => u.ContactRepository).Returns(new Mock<IRepository<Contact>>().Object);
        mock.Setup(u => u.MessageRepository).Returns(new Mock<IRepository<Message>>().Object);
        mock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        return mock;
    }

    /// <summary>
    /// Returns a <see cref="Mock{IRepository}"/> whose <c>GetAll()</c> returns the given sequence.
    /// </summary>
    public static Mock<IRepository<T>> WithGetAll<T>(IEnumerable<T> items) where T : class
    {
        var repo = new Mock<IRepository<T>>();
        repo.Setup(r => r.GetAll(It.IsAny<string[]>())).Returns(items.AsQueryable());
        return repo;
    }

    /// <summary>Shorthand: sets up <c>GetAll()</c> and <c>GetAsync()</c> for a known entity.</summary>
    public static Mock<IRepository<T>> WithGetAllAndGet<T>(IEnumerable<T> items, Guid id, T? entity) where T : class
    {
        var repo = WithGetAll(items);
        repo.Setup(r => r.GetAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        return repo;
    }
}

using WebMessenger.DAL.Entities;

namespace WebMessenger.DAL.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task CommitAsync(CancellationToken ct = default);
        Task BeginTransactionAsync(CancellationToken ct = default);
        Task CommitTransactionAsync(CancellationToken ct = default);

        IRepository<User> UserRepository { get; }
        IRepository<Chat> ChatRepository { get; }
        IRepository<ChatMember> ChatMemberRepository { get; }
        IRepository<Contact> ContactRepository { get; }
        IRepository<Message> MessageRepository { get; }
    }
}

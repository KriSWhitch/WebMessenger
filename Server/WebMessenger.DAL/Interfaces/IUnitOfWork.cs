using WebMessenger.DAL.Entities;

namespace WebMessenger.DAL.Interfaces
{
    public interface IUnitOfWork
    {
        void Commit();
        Task CommitAsync();
        IRepository<User> UserRepository { get; }
        IRepository<Chat> ChatRepository { get; }
        IRepository<ChatMember> ChatMemberRepository { get; }
        IRepository<Contact> ContactRepository { get; }
        IRepository<Message> MessageRepository { get; }
    }

}

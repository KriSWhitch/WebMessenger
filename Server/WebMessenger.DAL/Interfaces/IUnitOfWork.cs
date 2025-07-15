using WebMessenger.DAL.Entities;

namespace WebMessenger.DAL.Interfaces
{
    public interface IUnitOfWork
    {
        void Commit();
        Task CommitAsync();
        IRepository<User> UserRepository { get; }
    }

}

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using WebMessenger.DAL.Data;
using WebMessenger.DAL.Entities;
using WebMessenger.DAL.Interfaces;

namespace WebMessenger.DAL
{
    public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
    {
        private ApplicationDbContext Context { get; set; } = context;

        public void Commit()
        {
            try
            {
                Context.SaveChanges();
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Exception: {0} \n Inner Exception: {1}", ex.Message, ex.InnerException);
            }
        }

        public async Task CommitAsync()
        {
            try
            {
                await Context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Exception: {0} \n Inner Exception: {1}", ex.Message, ex.InnerException);
            }
        }

        public void Dispose() => Context.Dispose();

        private Repository<User>? _userRepository;
        public IRepository<User> UserRepository => _userRepository ??= new Repository<User>(Context);

        private Repository<Chat>? _chatRepository;
        public IRepository<Chat> ChatRepository => _chatRepository ??= new Repository<Chat>(Context);

        private Repository<ChatMember>? _chatMemberRepository;
        public IRepository<ChatMember> ChatMemberRepository => _chatMemberRepository ??= new Repository<ChatMember>(Context);

        private Repository<Contact>? _contactRepository;
        public IRepository<Contact> ContactRepository => _contactRepository ??= new Repository<Contact>(Context);

        private Repository<Message>? _messageRepository;
        public IRepository<Message> MessageRepository => _messageRepository ??= new Repository<Message>(Context);
    }
}

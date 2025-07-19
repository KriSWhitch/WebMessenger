using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using WebMessenger.DAL.Data;
using WebMessenger.DAL.Entities;
using WebMessenger.DAL.Interfaces;

namespace WebMessenger.DAL
{
    public class UnitOfWork : IUnitOfWork
    {
        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        private ApplicationDbContext _context { get; set; }

        public void Commit()
        {
            try
            {
                _context.SaveChanges();
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
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Exception: {0} \n Inner Exception: {1}", ex.Message, ex.InnerException);
            }
        }

        public void Dispose() => _context.Dispose();

        private Repository<User> _userRepository;
        public IRepository<User> UserRepository => _userRepository ??= new Repository<User>(_context);

        private Repository<Chat> _chatRepository;
        public IRepository<Chat> ChatRepository => _chatRepository ??= new Repository<Chat>(_context);

        private Repository<ChatMember> _chatMemberRepository;
        public IRepository<ChatMember> ChatMemberRepository => _chatMemberRepository ??= new Repository<ChatMember>(_context);

        private Repository<Contact> _contactRepository;
        public IRepository<Contact> ContactRepository => _contactRepository ??= new Repository<Contact>(_context);

        private Repository<Message> _messageRepository;
        public IRepository<Message> MessageRepository => _messageRepository ??= new Repository<Message>(_context);
    }
}

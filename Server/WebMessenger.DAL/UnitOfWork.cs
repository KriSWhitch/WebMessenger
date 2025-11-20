using Microsoft.EntityFrameworkCore.Storage;
using WebMessenger.DAL.Data;
using WebMessenger.DAL.Entities;
using WebMessenger.DAL.Interfaces;

namespace WebMessenger.DAL
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CommitAsync(CancellationToken ct = default)
        {
            await _context.SaveChangesAsync(ct);
        }

        public async Task BeginTransactionAsync(CancellationToken ct = default)
        {
            _transaction = await _context.Database.BeginTransactionAsync(ct);
        }

        public async Task CommitTransactionAsync(CancellationToken ct = default)
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync(ct);
                await _transaction.DisposeAsync();
            }
        }

        public void Dispose() => _context.Dispose();

        private Repository<User>? _userRepository;
        public IRepository<User> UserRepository => _userRepository ??= new Repository<User>(_context);

        private Repository<Chat>? _chatRepository;
        public IRepository<Chat> ChatRepository => _chatRepository ??= new Repository<Chat>(_context);

        private Repository<ChatMember>? _chatMemberRepository;
        public IRepository<ChatMember> ChatMemberRepository => _chatMemberRepository ??= new Repository<ChatMember>(_context);

        private Repository<Contact>? _contactRepository;
        public IRepository<Contact> ContactRepository => _contactRepository ??= new Repository<Contact>(_context);

        private Repository<Message>? _messageRepository;
        public IRepository<Message> MessageRepository => _messageRepository ??= new Repository<Message>(_context);
    }
}
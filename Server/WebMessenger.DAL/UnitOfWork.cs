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
        public IRepository<User> UserRepository => _userRepository ?? (_userRepository = new Repository<User>(_context));
    }
}

using Microsoft.EntityFrameworkCore;
using WebMessenger.DAL.Interfaces;

namespace WebMessenger.DAL
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly DbSet<T> _dbSet;
        private readonly DbContext _context;

        public Repository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public IQueryable<T> GetAll(params string[] navigationProperties)
        {
            var query = _dbSet.AsNoTracking();
            foreach (var navProp in navigationProperties)
            {
                query = query.Include(navProp);
            }
            return query;
        }

        public async Task<T?> GetAsync(Guid id, CancellationToken ct = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, ct);
        }

        public async Task InsertAsync(T entity, CancellationToken ct = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            await _dbSet.AddAsync(entity, ct);
        }

        public Task UpdateAsync(T entity, CancellationToken ct = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _dbSet.FindAsync(new object[] { id }, ct);
            if (entity == null) throw new KeyNotFoundException($"Entity {typeof(T).Name} with id {id} not found");
            _dbSet.Remove(entity);
        }

        public async Task CreateRangeAsync(IEnumerable<T> items, CancellationToken ct = default)
        {
            await _dbSet.AddRangeAsync(items, ct);
        }

        public Task DeleteRangeAsync(IEnumerable<T> items, CancellationToken ct = default)
        {
            _dbSet.RemoveRange(items);
            return Task.CompletedTask;
        }
    }
}
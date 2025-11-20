namespace WebMessenger.DAL.Interfaces
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> GetAll(params string[] navigationProperties);
        Task<T?> GetAsync(Guid id, CancellationToken ct = default);
        Task InsertAsync(T entity, CancellationToken ct = default);
        Task UpdateAsync(T entity, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
        Task CreateRangeAsync(IEnumerable<T> items, CancellationToken ct = default);
        Task DeleteRangeAsync(IEnumerable<T> items, CancellationToken ct = default);
    }
}
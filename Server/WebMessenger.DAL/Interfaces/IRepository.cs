namespace WebMessenger.DAL.Interfaces
{
    public interface IRepository<T> where T : class
    {
        void Insert(T entity);
        void Delete(int id);
        void Update(T entity);
        T Get(int id);
        IQueryable<T> GetAll();
        IQueryable<T> GetAll(params string[] navigationProperties);
        void DeleteRange(IEnumerable<T> items);
        void CreateRange(IEnumerable<T> items);
    }

}

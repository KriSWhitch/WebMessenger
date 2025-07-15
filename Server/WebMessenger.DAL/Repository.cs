using Microsoft.EntityFrameworkCore;
using WebMessenger.DAL.Interfaces;

namespace WebMessenger.DAL
{
    public class Repository<T> : IRepository<T> where T : class
    {
        internal DbSet<T> DbSet;
        internal DbContext Context;
        public Repository(DbContext context)
        {
            Context = context;
            DbSet = context.Set<T>();
        }

        public void Insert(T entity)
        {
            if (entity != null)
            {
                DbSet.Add(entity);
            }
            else
            {
                throw new ArgumentNullException();
            }
        }

        public void Delete(int id)
        {
            var entity = DbSet.Find(id);
            if (entity != null)
            {
                DbSet.Attach(entity);
                DbSet.Remove(entity);
            }
            else
            {
                throw new ArgumentNullException();
            }
        }

        public void Update(T entity)
        {
            if (entity != null)
            {
                DbSet.Attach(entity);
                Context.Entry(entity).State = EntityState.Modified;
            }
            else
            {
                throw new ArgumentNullException();
            }
        }

        public T Get(int id)
        {
            return DbSet.Find(id);
        }

        public IQueryable<T> GetAll()
        {
            return DbSet;
        }

        public IQueryable<T> GetAll(params string[] navigationProperties)
        {
            var query = Context.Set<T>().AsQueryable();

            foreach (var navProp in navigationProperties)
            {
                query = query.Include(navProp);
            }

            return query;
        }

        public void DeleteRange(IEnumerable<T> items)
        {
            Context.Set<T>().RemoveRange(items);
        }

        public void CreateRange(IEnumerable<T> items)
        {
            Context.Set<T>().AddRange(items);
        }
    }

}

using System.Linq.Expressions;

namespace Cursus.Domain.Interfaces.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        IQueryable<T> GetById(int id);
        IQueryable<T> GetAll();
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
        Task<int> SaveChangesAsync();
    }
}
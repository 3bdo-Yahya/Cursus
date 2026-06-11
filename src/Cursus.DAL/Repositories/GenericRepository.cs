using System.Linq.Expressions;

namespace Cursus.DAL.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _dbContext;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = _dbContext.Set<T>();
        }

        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);
        public async Task<int> CountAsync() => await _dbSet.CountAsync();
        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate) => await _dbSet.CountAsync(predicate);
        public void Delete(T entity) => _dbSet.Remove(entity);
        public IQueryable<T> GetAll() => _dbSet.AsNoTracking();
        public IQueryable<T> GetById(int id) => _dbSet.Where(e => EF.Property<int>(e, "Id") == id);
        public void Update(T entity) => _dbSet.Update(entity);
        public async Task<int> SaveChangesAsync() => await _dbContext.SaveChangesAsync();
    }
}
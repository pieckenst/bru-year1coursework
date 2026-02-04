using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TicketSalesApp.Core.Data;

namespace TicketSalesApp.Services.Implementations
{
    /// <summary>
    /// Generic repository implementation for Entity Framework
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly DbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            var entry = await _dbSet.AddAsync(entity);
            return entry.Entity;
        }

        public virtual Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            return Task.CompletedTask;
        }

        public virtual Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            if (predicate == null)
                return await _dbSet.CountAsync();
            
            return await _dbSet.CountAsync(predicate);
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? filter = null)
        {
            IQueryable<T> query = _dbSet;
            
            if (filter != null)
                query = query.Where(filter);
            
            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            return entities;
        }

        public virtual Task UpdateRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
            return Task.CompletedTask;
        }

        public virtual Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
            return Task.CompletedTask;
        }

        public virtual async Task<T?> GetWithIncludesAsync(object id, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;
            
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            
            return await query.FirstOrDefaultAsync(GetKeyPredicate(id));
        }

        public virtual async Task<IEnumerable<T>> FindWithIncludesAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;
            
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            
            return await query.Where(predicate).ToListAsync();
        }

        /// <summary>
        /// Creates a predicate for finding entity by primary key
        /// This is a simplified implementation - in production, you might want to use reflection
        /// to handle composite keys or different key types
        /// </summary>
        protected virtual Expression<Func<T, bool>> GetKeyPredicate(object id)
        {
            // This is a simplified implementation
            // In a real-world scenario, you'd want to use reflection to find the primary key property
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, "Id"); // Assumes property is named "Id"
            var constant = Expression.Constant(id);
            var equal = Expression.Equal(property, constant);
            
            return Expression.Lambda<Func<T, bool>>(equal, parameter);
        }
    }
}
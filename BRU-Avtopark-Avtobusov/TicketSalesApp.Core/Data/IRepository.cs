#if MODERN
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace TicketSalesApp.Core.Data
{
    /// <summary>
    /// Generic repository interface for data access operations
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IRepository<T> where T : class
    {
        // Basic CRUD operations
        Task<T?> GetByIdAsync(object id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        
        // Advanced operations
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? filter = null);
        
        // Bulk operations
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
        Task UpdateRangeAsync(IEnumerable<T> entities);
        Task DeleteRangeAsync(IEnumerable<T> entities);
        
        // Include operations for Entity Framework
        Task<T?> GetWithIncludesAsync(object id, params Expression<Func<T, object>>[] includes);
        Task<IEnumerable<T>> FindWithIncludesAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
    }
}
#endif
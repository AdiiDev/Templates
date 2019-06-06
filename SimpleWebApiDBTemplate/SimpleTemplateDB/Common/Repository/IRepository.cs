using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.Repository
{
    /// <summary>
    /// Base for Repository. 
    /// </summary>
    /// <typeparam name="T">Class that inherit from IBaseEntity<E></typeparam>
    public interface IRepository<T> where T : class
    {
        Task SaveAsync(T entity);

        Task SaveAsync(IList<T> entities);

        Task RemoveAsync(T entity);

        Task RemoveAsync(IList<T> entities);

        Task FlushAsync();

        Task<T> FindAsync(int entityId);

        IQueryable<T> All();

        Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, int pageNumber, int pageSize);
        Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> sorting, ListSortDirection orderDirection = ListSortDirection.Ascending, bool sortBefore = false);
        Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, int pageNumber, int pageSize, Expression<Func<T,object>> sorting, ListSortDirection orderDirection = ListSortDirection.Ascending , bool sortBefore = false);

        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> sorting, ListSortDirection orderDirection = ListSortDirection.Ascending, bool sortBefore = false);

        Task<object> ExecuteCommand(string command);

    }
}

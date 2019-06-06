using Common.UOW;
using NHibernate;
using NHibernate.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly UnitOfWork _uow;
        protected ISession Session { get { return _uow.Session; } }

        public Repository(IUnitOfWork unitOfWork)
        {
            _uow = (UnitOfWork)unitOfWork;
        }

        /// <summary>
        /// Save object async
        /// </summary>
        /// <param name="entity">Object to save</param>
        /// <returns></returns>
        public virtual async Task SaveAsync(T entity)
        {
            await Session.SaveOrUpdateAsync(entity);
        }

        /// <summary>
        /// Save objects async
        /// </summary>
        /// <param name="entities">Objects to save</param>
        /// <returns></returns>
        public virtual async Task SaveAsync(IList<T> entities)
        {
            foreach (T entity in entities)
            {
                await Session.SaveOrUpdateAsync(entity);
            }
        }

        /// <summary>
        /// Remove object async
        /// </summary>
        /// <param name="entity">Object to delete</param>
        /// <returns></returns>
        public virtual async Task RemoveAsync(T entity)
        {
            await Session.DeleteAsync(entity);
        }

        /// <summary>
        /// Remove objects async
        /// </summary>
        /// <param name="entities">Objects to delete</param>
        /// <returns></returns>
        public virtual async Task RemoveAsync(IList<T> entities)
        {
            foreach (var entity in entities)
            {
                await Session.DeleteAsync(entity);
            }
        }

        /// <summary>
        /// WARNING: Don't user until you really need. Flush method. 
        /// </summary>
        /// <returns></returns>
        public virtual async Task FlushAsync()
        {
            await Session.FlushAsync();
        }

        /// <summary>
        /// Get total number of rows
        /// </summary>
        /// <returns>Total number of rows</returns>
        public int GetTotalNumber()
        {
            return All().Count();
        }

        /// <summary>
        /// Find async by entity id
        /// </summary>
        /// <param name="entityId">Entity id</param>
        /// <returns>Element</returns>
        public async Task<T> FindAsync(int entityId)
        {
            return await Session.GetAsync<T>(entityId);
        }

        /// <summary>
        /// Get all rows
        /// </summary>
        /// <returns></returns>
        public IQueryable<T> All()
        {
            return Session.Query<T>();
        }

        /// <summary>
        /// Get all rows 
        /// </summary>
        /// <returns></returns>
        public async Task<List<T>> FindAllAsync()
        {
            return await All().ToListAsync();
        }

        /// <summary>
        /// Get number of rows
        /// </summary>
        /// <returns></returns>
        public async Task<int> CountAsync()
        {
            return await All().CountAsync();
        }

        /// <summary>
        /// Get number of rows that satisfy predicate
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <returns></returns>
        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await All().Where(predicate).CountAsync();
        }

        /// <summary>
        /// Get number of rows that satisfy predicate with sort
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="sorting">Sorting</param>
        /// <param name="orderDirection">Order direction <see cref="ListSortDirection"/></param>
        /// <param name="sortBefore">If true then method first sort then use predicate. False first predicate then sort</param>
        /// <returns></returns>
        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> sorting, ListSortDirection orderDirection = ListSortDirection.Ascending, bool sortBefore = false)
        {
            IQueryable<T> query = All();
            if (sortBefore)
            {
                query = orderDirection == ListSortDirection.Ascending ? query.OrderBy(sorting) : query.OrderByDescending(sorting);
                query = query.Where(predicate);

                return await query.CountAsync();
            }
            else
            {
                query = query.Where(predicate);
                query = orderDirection == ListSortDirection.Ascending ? query.OrderBy(sorting) : query.OrderByDescending(sorting);

                return await query.CountAsync();
            }
        }

        /// <summary>
        /// Return entities that satisfy predicate
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <returns></returns>
        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            IQueryable<T> query = All().Where(predicate);

            return await query.ToListAsync();
        }

        /// <summary>
        /// Return entities that satisfy predicate with pagging
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns></returns>
        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, int pageNumber, int pageSize)
        {
            if (pageNumber <= 0 || pageSize <= 0)
                return await FindAsync(predicate);

            IQueryable<T> query = All()
                .Where(predicate)
                .Skip(pageNumber * pageSize)
                .Take(pageSize);

            return await query.ToListAsync();
        }

        /// <summary>
        /// Return entities that satisfy predicate with sorting and pagging
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="sorting">Sorting</param>
        /// <param name="orderDirection">Order direction <see cref="ListSortDirection"/></param>
        /// <param name="sortBefore">If true then method first sort then use predicate. False first predicate then sort</param>
        /// <returns></returns>
        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, int pageNumber, int pageSize, Expression<Func<T, object>> sorting, ListSortDirection orderDirection = ListSortDirection.Ascending, bool sortBefore = false)
        {
            IQueryable<T> query = All();

            if (sortBefore)
            {
                query = orderDirection == ListSortDirection.Ascending ? query.OrderBy(sorting) : query.OrderByDescending(sorting);
                query = query.Where(predicate)
                    .Skip(pageNumber * pageSize)
                    .Take(pageSize);

                return await query.ToListAsync();
            }
            else
            {
                query = query.Where(predicate);
                query = orderDirection == ListSortDirection.Ascending ? query.OrderBy(sorting) : query.OrderByDescending(sorting)
                    .Skip(pageNumber * pageSize)
                    .Take(pageSize);

                return await query.ToListAsync();
            }

        }

        /// <summary>
        /// Return entities that satisfy predicate and sort them
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="sorting">Sorting</param>
        /// <param name="orderDirection">Order direction <see cref="ListSortDirection"/></param>
        /// <param name="sortBefore">If true then method first sort then use predicate. False first predicate then sort</param>
        /// <returns></returns>
        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> sorting, ListSortDirection orderDirection = ListSortDirection.Ascending, bool sortBefore = false)
        {
            IQueryable<T> query = All();
            if(sortBefore)
            {
                query = orderDirection == ListSortDirection.Ascending ? query.OrderBy(sorting) : query.OrderByDescending(sorting);
                query = query.Where(predicate);

                return await query.ToListAsync();
            }
            else
            {
                query = query.Where(predicate);
                query = orderDirection == ListSortDirection.Ascending ? query.OrderBy(sorting) : query.OrderByDescending(sorting);

                return await query.ToListAsync();
            }
        }

        /// <summary>
        /// This method execute function in db
        /// </summary>
        /// <param name="command">SQL command</param>
        /// <returns>Query result</returns>
        public async Task<Object> ExecuteCommand(string command)
        {
            var query = Session.CreateSQLQuery(command);

            await query.ExecuteUpdateAsync();

            return await query.UniqueResultAsync();
        }
    }
}

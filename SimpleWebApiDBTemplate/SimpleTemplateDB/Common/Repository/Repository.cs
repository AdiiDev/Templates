using Common.DomainBase;
using Common.UOW;
using FluentNHibernate.Cfg;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
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

        public virtual async Task SaveAsync(T entity)
        {
            await Session.SaveOrUpdateAsync(entity);
        }

        public virtual async Task SaveAsync(IList<T> entities)
        {
            foreach (T entity in entities)
            {
                await Session.SaveOrUpdateAsync(entity);
            }
        }

        public virtual async Task RemoveAsync(T entity)
        {
            await Session.DeleteAsync(entity);
        }

        public virtual async Task RemoveAsync(IList<T> entities)
        {
            foreach (var entity in entities)
            {
                await Session.DeleteAsync(entity);
            }
        }

        public virtual async Task FlushAsync()
        {
            await Session.FlushAsync();
        }

        public int GetTotalNumber()
        {
            return All().Count();
        }

        public async Task<T> FindAsync(int entityId)
        {
            return await Session.GetAsync<T>(entityId);
        }

        public IQueryable<T> All()
        {
            return Session.Query<T>();
        }

        public async Task<List<T>> FindAllAsync()
        {
            return await All().ToListAsync();
        }

        public async Task<int> CountAsync()
        {
            return await All().CountAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await All().Where(predicate).CountAsync();
        }

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

        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            IQueryable<T> query = All().Where(predicate);

            return await query.ToListAsync();
        }

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


        public async Task<Object> ExecuteCommand(string command)
        {
            var query = Session.CreateSQLQuery(command);

            await query.ExecuteUpdateAsync();

            return await query.UniqueResultAsync();
        }
    }
}

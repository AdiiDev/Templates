using FluentNHibernate.Cfg;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using System;
using System.Reflection;

namespace Common.UOW
{
    public class UnitOfWork : IUnitOfWork
    {
        private static readonly ISessionFactory _sessionFactory;
        private ITransaction _transaction;
        public ISession Session { get; set; }

        static UnitOfWork()
        {
            //Assemblies to Load with mappings
            string[] AssembliesWithMappings = new[] { "Template.Api" };

            //Getting nhibernate nofiguration file from running assembly
            var configuration = new NHibernate.Cfg.Configuration().Configure(AppDomain.CurrentDomain.BaseDirectory + "\\hibernateSQL.cfg.xml");

            _sessionFactory = Fluently.Configure(configuration)
                //Mappings
                .Mappings(m =>
                {
                    foreach (var assembly in AssembliesWithMappings)
                    {
                        m.FluentMappings
                            .AddFromAssembly(Assembly.Load(assembly));
                    }
                })
                .CurrentSessionContext("call")
                //This should create and updates tables in db. WARNING: SchemaUpdate can only CREATE and UPDATE. DELETE and DROP options are not avaiable
                .ExposeConfiguration(cfg => new SchemaUpdate(cfg).Execute(true, true))
                .BuildSessionFactory();
        }

        public UnitOfWork()
        {
            Session = _sessionFactory.OpenSession();
        }

        public void BeginTransaction()
        {
            _transaction = Session.BeginTransaction();
        }

        public void Commit()
        {
            try
            {
                if (_transaction != null && _transaction.IsActive)
                    _transaction.Commit();
            }
            catch
            {
                if (_transaction != null && _transaction.IsActive)
                    _transaction.Rollback();

                throw;
            }
            finally
            {
                Session.Dispose();
            }
        }

        public void Rollback()
        {
            try
            {
                if (_transaction != null && _transaction.IsActive)
                    _transaction.Rollback();
            }
            finally
            {
                Session.Dispose();
            }
        }
    }
}

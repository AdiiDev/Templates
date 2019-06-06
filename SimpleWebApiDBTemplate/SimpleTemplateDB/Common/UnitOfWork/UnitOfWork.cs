using FluentNHibernate.Cfg;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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
            string[] AssembliesWithMappings = new[] { "SimpleTemplateDB" };

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
                //This should create and updates tables in db
                //.ExposeConfiguration(cfg => new SchemaExport(cfg).Execute(true, true, false))
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

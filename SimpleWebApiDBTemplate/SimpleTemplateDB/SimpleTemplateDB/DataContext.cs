using Microsoft.EntityFrameworkCore;
using SimpleTemplateDB.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleTemplateDB
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        public DbSet<Person> Persons {get;set;}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<User.Domain.User.UserModel>().
            base.OnModelCreating(modelBuilder);
        }
    }
}

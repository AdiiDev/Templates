using Common.Repository;
using SimpleTemplateDB.Entity;

namespace SimpleTemplateDB
{
    public interface IPersonService : IRepository<Person>
    {
    }
}

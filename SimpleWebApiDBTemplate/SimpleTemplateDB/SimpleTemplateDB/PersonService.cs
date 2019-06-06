using Common.Repository;
using Common.UOW;
using SimpleTemplateDB.Entity;

namespace SimpleTemplateDB
{
    public class PersonService : Repository<Person>, IPersonService
    {
        public PersonService(IUnitOfWork _uow) : base(_uow)
        {
            
        }
    }
}

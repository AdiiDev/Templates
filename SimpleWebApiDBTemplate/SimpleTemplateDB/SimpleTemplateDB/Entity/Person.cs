using Common.DomainBase;

namespace SimpleTemplateDB.Entity
{
    public class Person : IBaseEntity<int>
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
    }
}

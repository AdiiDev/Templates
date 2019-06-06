using FluentNHibernate.Mapping;

namespace SimpleTemplateDB.Entity
{
    public class PersonMappings : ClassMap<Person>
    {
        public PersonMappings()
        {
            Table("Osoby");
            Id(e => e.Id).GeneratedBy.Identity();
            Map(e => e.Name).Column("imie");
        }
    }
}

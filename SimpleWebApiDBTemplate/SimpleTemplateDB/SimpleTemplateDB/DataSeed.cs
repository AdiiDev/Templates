using System;
using System.Threading.Tasks;
using SimpleTemplateDB.Entity;

namespace SimpleTemplateDB
{
    public static class DataSeed
    {
        public const int NumberOfRows = 100;
        public static async Task InitDb(IPersonService personService)
        {
            var count = await personService.CountAsync();
            if (count <= 0)
            {
                Console.WriteLine("Auto init db");
                for (int i = 0; i < NumberOfRows; i++)
                {
                    await personService.SaveAsync(new Person {Name = $"person{i}", Random = i});
                }
            }
            else
                Console.WriteLine("Table is already filled");
        }
    }
}

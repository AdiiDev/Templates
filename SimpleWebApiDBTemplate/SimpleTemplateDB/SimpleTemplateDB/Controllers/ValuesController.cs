using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SimpleTemplateDB.Entity;

namespace SimpleTemplateDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly IPersonService personService;

        public ValuesController(IPersonService _personService)
        {
            personService = _personService;
        }

        // GET api/values
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> Get()
        {
            Person test = new Person { Name = "tester42" };
            await personService.SaveAsync(test);

            //Uncomment to see that when exception is thrown test42 is not saved in database
            //throw new Exception();
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> Get(int id)
        {
            var test = await personService.FindAsync(x => x.Random > 90);
            var test2 = await personService.FindAsync((x => x.Random > 90), (x => x.Random), ListSortDirection.Descending);
            var test3 = await personService.FindAsync((x => x.Random > 90), (x => x.Random), ListSortDirection.Descending, true);
            var test4 = await personService.FindAsync((x => x.Id > 0), 2, 10);
            var test5 = await personService.FindAsync((x => x.Id > 0), 12, 10);

            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
            var test22 = new TimeSpanConverter();
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

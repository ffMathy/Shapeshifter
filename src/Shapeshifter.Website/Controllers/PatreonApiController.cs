using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;

namespace Shapeshifter.Website.Controllers
{
  [Produces("application/json")]
  [Route("api/patreon")]
  public class PatreonApiController : Controller
  {
    private readonly IPatreonClient _client;

    public PatreonApiController(
      IConfigurationReader configurationReader)
    {
      _client = new PatreonClient(configurationReader.Read("patreon.creatorsAccessToken"));
    }

    // GET: api/Patreon
    [HttpGet]
    public IEnumerable<string> GetSupporters()
    {
      return new string[] { "value1", "value2" };
    }

    // GET: api/Patreon/5
    [HttpGet("{id}", Name = "Get")]
    public string Get(int id)
    {
      return "value";
    }

    // POST: api/Patreon
    [HttpPost]
    public void Post([FromBody]string value)
    {
    }

    // PUT: api/Patreon/5
    [HttpPut("{id}")]
    public void Put(int id, [FromBody]string value)
    {
    }

    // DELETE: api/ApiWithActions/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }
  }
}

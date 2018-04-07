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
  }
}

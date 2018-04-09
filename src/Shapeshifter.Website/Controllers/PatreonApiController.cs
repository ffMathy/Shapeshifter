using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    // GET: api/patreon/pledges
    [HttpGet("supporters")]
    public async Task<IEnumerable<string>> GetSupporters()
    {
		var pledges = await _client.GetPledgesAsync();
		foreach(var pledge in pledges) {
			var user = await _client.GetUserById(pledge.Relationships.Patron.Data.Id);
			user = null;
		}

		return new [] { "foo", "bar"};
    }
  }
}

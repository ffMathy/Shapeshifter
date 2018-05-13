using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Shapeshifter.Website.Controllers
{
	using Logic;

	using Models.Patreon.Response;

	[Produces("application/json")]
  [Route("api/patreon")]
  public class PatreonApiController : Controller
  {
	  readonly IPatreonClient _client;

    public PatreonApiController(
      IConfigurationReader configurationReader)
    {
      _client = new PatreonClient(configurationReader.Read("patreon.creatorsAccessToken"));
    }
	
    [HttpGet("supporters")]
    public async Task<IEnumerable<dynamic>> GetSupporters()
    {
		var supporters = new List<dynamic>();

		var pledges = await _client.GetPledgesAsync();
		var users = pledges.Included.ToObject<User[]>();
		foreach (var pledge in pledges.Data) {
			var user = users.Single(x => x.Id == pledge.Relationships.Patron.Data.Id);
			if(user.Attributes.IsDeleted || user.Attributes.IsSuspended || user.Attributes.IsNuked)
				continue;

			supporters.Add(new {
				user.Id,
				user.Attributes.FullName,
				Amount = pledge.Attributes.AmountCents / 100.0,
				user.Attributes.ImageUrl,
				user.Attributes.Url
			});
		}

		return supporters;
    }
  }
}

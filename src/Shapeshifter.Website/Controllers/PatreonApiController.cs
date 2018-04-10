﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Shapeshifter.Website.Models;

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
    public async Task<IEnumerable<dynamic>> GetSupporters()
    {
		var supporters = new List<dynamic>();

		var pledges = await _client.GetPledgesAsync();
		var users = pledges.Included.ToObject<User[]>();
		foreach (var pledge in pledges.Data) {
			var user = users.Single(x => x.Id == pledge.Relationships.Patron.Data.Id);
			supporters.Add(new {
				user.Id,
				user.Attributes.FullName,
				Amount = pledge.Attributes.AmountCents / 100.0
			});
		}

		return supporters;
    }
  }
}

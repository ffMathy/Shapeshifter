using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shapeshifter.Website
{
	using FluffySpoon.Http;
	using Models;
  using System.Net.Http;

  public class PatreonClient : IPatreonClient
  {
	readonly IRestClient _restClient;

    const int campaignId = 557794;

    readonly string accessToken;

    public PatreonClient(
        string accessToken)
    {
      this.accessToken = accessToken;

	  _restClient = new RestClient();
    }

    public async Task<IEnumerable<Pledge>> GetPledges()
    {
		_restClient.GetAsync<DataWrapper<Pledge>>();
    }
  }
}

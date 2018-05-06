namespace Shapeshifter.Website.Logic
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Threading.Tasks;

	using FluffySpoon.Http;

	using Models.Patreon.Response;

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

		public async Task<DataWrapper<Pledge[]>> GetPledgesAsync()
		{
			var pledges = await _restClient.GetAsync<DataWrapper<Pledge[]>>(
				new Uri("https://www.patreon.com/api/oauth2/api/campaigns/" + campaignId + "/pledges"),
				new Dictionary<HttpRequestHeader, string>{
					{ HttpRequestHeader.Authorization, "Bearer " + accessToken }
				});
			return pledges;
		}
	}
}

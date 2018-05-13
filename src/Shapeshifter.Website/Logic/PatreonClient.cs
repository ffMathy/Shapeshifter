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
		readonly ISettingsManager settingsManager;
		readonly IRestClient _restClient;

		const int campaignId = 557794;

		string accessToken;
		string refreshToken;

		readonly string clientId;
		readonly string clientSecret;

		public PatreonClient(
			IConfigurationReader configurationReader,
			ISettingsManager settingsManager)
		{
			this.settingsManager = settingsManager;

			accessToken =
				settingsManager.LoadSetting<string>("patreon.accessToken") ??
				configurationReader.Read("patreon.creatorsAccessToken");

			refreshToken =
				settingsManager.LoadSetting<string>("patreon.refreshToken") ??
				configurationReader.Read("patreon.creatorsRefreshToken");

			clientId = configurationReader.Read("patreon.clientId");
			clientSecret = configurationReader.Read("patreon.clientSecret");

			_restClient = new RestClient();
		}

		public async Task<DataWrapper<Pledge[]>> GetPledgesAsync()
		{
			var pledges = await _restClient.GetAsync<DataWrapper<Pledge[]>>(
				new Uri("https://www.patreon.com/api/oauth2/api/campaigns/" + campaignId + "/pledges"),
				GetAuthorizationHeader());
			return pledges;
		}

		public async Task RefreshTokenAsync()
		{
			var tokenResponse = await _restClient.PostAsync<dynamic>(
				new Uri($"https://www.patreon.com/api/oauth2/token?grant_type=refresh_token&refresh_token={refreshToken}&client_id={clientId}&client_secret={clientSecret}"),
				GetAuthorizationHeader());

			settingsManager.SaveSetting(
				"patreon.accessToken",
				accessToken = tokenResponse.access_token);

			settingsManager.SaveSetting(
				"patreon.refreshToken",
				refreshToken = tokenResponse.refresh_token);
		}

		Dictionary<HttpRequestHeader, string> GetAuthorizationHeader()
		{
			return new Dictionary<HttpRequestHeader, string>{
				{ HttpRequestHeader.Authorization, "Bearer " + accessToken }
			};
		}
	}
}

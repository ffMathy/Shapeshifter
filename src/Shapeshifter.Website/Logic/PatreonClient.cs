namespace Shapeshifter.Website.Logic;

using System.Net.Http.Headers;

using Microsoft.Extensions.Options;

using Models.Patreon.Response;

using Newtonsoft.Json;

public sealed class PatreonTokenResponse
{
	[JsonProperty("access_token")]
	public string? AccessToken { get; set; }

	[JsonProperty("refresh_token")]
	public string? RefreshToken { get; set; }

	[JsonProperty("expires_in")]
	public long? ExpiresIn { get; set; }

	[JsonProperty("token_type")]
	public string? TokenType { get; set; }

	[JsonProperty("scope")]
	public string? Scope { get; set; }
}

/// <summary>
/// Talks to the Patreon API on behalf of the campaign creator. Registered as a singleton so that the
/// access token, its expiry and the (rotating) refresh token are shared by all requests.
/// </summary>
public sealed class PatreonClient(
	IHttpClientFactory httpClientFactory,
	ISettingsManager settingsManager,
	IOptions<PatreonOptions> options,
	TimeProvider timeProvider) : IPatreonClient, IDisposable
{
	public const string HttpClientName = "Patreon";

	const string AccessTokenSettingKey = "patreon.accessToken";
	const string RefreshTokenSettingKey = "patreon.refreshToken";

	static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(5);

	readonly SemaphoreSlim _tokenLock = new(1, 1);

	string? _accessToken;
	string? _refreshToken;
	bool _tokensLoaded;

	// Unknown until the first refresh in this process, so the first call always refreshes
	// (as the old site did on every call) and later calls reuse the token until it nearly expires.
	DateTimeOffset _accessTokenExpiresAt = DateTimeOffset.MinValue;

	PatreonOptions Options => options.Value;

	public async Task<DataWrapper<Pledge[]>> GetPledgesAsync(CancellationToken cancellationToken)
	{
		var accessToken = _accessToken ?? throw new InvalidOperationException(
			"No Patreon access token. Call EnsureAccessTokenAsync first.");

		using var request = new HttpRequestMessage(
			HttpMethod.Get,
			new Uri($"api/oauth2/api/campaigns/{Options.CampaignId}/pledges", UriKind.Relative));
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

		using var client = httpClientFactory.CreateClient(HttpClientName);
		using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();

		var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		return JsonConvert.DeserializeObject<DataWrapper<Pledge[]>>(json)
			?? throw new InvalidOperationException("Patreon returned an empty pledges response.");
	}

	public async Task EnsureAccessTokenAsync(CancellationToken cancellationToken)
	{
		await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			LoadPersistedTokens();

			if (_accessToken != null && timeProvider.GetUtcNow() < _accessTokenExpiresAt - ExpiryMargin)
				return;

			if (string.IsNullOrWhiteSpace(_refreshToken))
				throw new InvalidOperationException("No Patreon refresh token is configured (Patreon:CreatorsRefreshToken).");

			var token = await RequestTokenAsync(
				new Dictionary<string, string>
				{
					["grant_type"] = "refresh_token",
					["refresh_token"] = _refreshToken,
				},
				cancellationToken).ConfigureAwait(false);

			if (string.IsNullOrEmpty(token.AccessToken))
				throw new InvalidOperationException("Patreon did not return an access token.");

			_accessToken = token.AccessToken;
			settingsManager.SaveSetting(AccessTokenSettingKey, _accessToken);

			// Patreon rotates refresh tokens; the old one stops working once a new one is issued.
			if (!string.IsNullOrEmpty(token.RefreshToken))
			{
				_refreshToken = token.RefreshToken;
				settingsManager.SaveSetting(RefreshTokenSettingKey, _refreshToken);
			}

			_accessTokenExpiresAt = token.ExpiresIn is { } seconds and > 0
				? timeProvider.GetUtcNow().AddSeconds(seconds)
				: timeProvider.GetUtcNow().AddHours(1);
		}
		finally
		{
			_tokenLock.Release();
		}
	}

	public Task<PatreonTokenResponse> ExchangeAuthorizationCodeAsync(
		string code,
		Uri redirectUri,
		CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrEmpty(code);
		ArgumentNullException.ThrowIfNull(redirectUri);

		return RequestTokenAsync(
			new Dictionary<string, string>
			{
				["grant_type"] = "authorization_code",
				["code"] = code,
				["redirect_uri"] = redirectUri.AbsoluteUri,
			},
			cancellationToken);
	}

	public void Dispose() => _tokenLock.Dispose();

	void LoadPersistedTokens()
	{
		if (_tokensLoaded)
			return;

		_accessToken = NullIfEmpty(settingsManager.LoadSetting<string>(AccessTokenSettingKey))
			?? NullIfEmpty(Options.CreatorsAccessToken);
		_refreshToken = NullIfEmpty(settingsManager.LoadSetting<string>(RefreshTokenSettingKey))
			?? NullIfEmpty(Options.CreatorsRefreshToken);
		_tokensLoaded = true;
	}

	async Task<PatreonTokenResponse> RequestTokenAsync(
		Dictionary<string, string> parameters,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(Options.ClientId) || string.IsNullOrWhiteSpace(Options.ClientSecret))
			throw new InvalidOperationException("Patreon:ClientId and Patreon:ClientSecret must be configured.");

		parameters["client_id"] = Options.ClientId;
		parameters["client_secret"] = Options.ClientSecret;

		// Patreon's token endpoint accepts a standard application/x-www-form-urlencoded body,
		// so the client secret and tokens are not put in the URL (where they would end up in logs).
		using var content = new FormUrlEncodedContent(parameters);
		using var client = httpClientFactory.CreateClient(HttpClientName);
		using var response = await client
			.PostAsync(new Uri("api/oauth2/token", UriKind.Relative), content, cancellationToken)
			.ConfigureAwait(false);
		response.EnsureSuccessStatusCode();

		var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		return JsonConvert.DeserializeObject<PatreonTokenResponse>(json)
			?? throw new InvalidOperationException("Patreon returned an empty token response.");
	}

	static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

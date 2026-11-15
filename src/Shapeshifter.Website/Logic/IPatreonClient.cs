namespace Shapeshifter.Website.Logic;

using Models.Patreon.Response;

public interface IPatreonClient
{
	/// <summary>Fetches the creator campaign's pledges (with the pledging users in <c>included</c>).</summary>
	Task<DataWrapper<Pledge[]>> GetPledgesAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Makes sure a valid creator access token is available, refreshing (and persisting the rotated
	/// refresh token) when it is unknown or about to expire. Concurrent callers share one refresh.
	/// </summary>
	Task EnsureAccessTokenAsync(CancellationToken cancellationToken);

	/// <summary>Exchanges an OAuth authorization code (from the Patreon redirect) for tokens.</summary>
	Task<PatreonTokenResponse> ExchangeAuthorizationCodeAsync(string code, Uri redirectUri, CancellationToken cancellationToken);
}

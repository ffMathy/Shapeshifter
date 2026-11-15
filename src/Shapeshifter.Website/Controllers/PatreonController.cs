namespace Shapeshifter.Website.Controllers;

using Logic;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

/// <summary>
/// OAuth redirect target used once by the creator to obtain the creator tokens
/// (to be stored as Patreon:CreatorsAccessToken / Patreon:CreatorsRefreshToken).
/// </summary>
public sealed class PatreonController(
	IPatreonClient client,
	IOptions<PatreonOptions> options) : Controller
{
	[HttpGet("Patreon/Postback")]
	public async Task<IActionResult> Postback(string? code, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(options.Value.ClientId) || string.IsNullOrWhiteSpace(options.Value.ClientSecret))
		{
			return Problem(
				statusCode: StatusCodes.Status503ServiceUnavailable,
				detail: "The Patreon integration is not configured on this server.");
		}

		if (string.IsNullOrWhiteSpace(code))
			return BadRequest("Missing OAuth 'code' parameter.");

		// The redirect URI must match the one used in the authorize request: this URL without its query string.
		var redirectUri = new Uri(UriHelper.BuildAbsolute(
			Request.Scheme,
			Request.Host,
			Request.PathBase,
			Request.Path));

		var token = await client.ExchangeAuthorizationCodeAsync(code, redirectUri, cancellationToken);

		Response.Headers.CacheControl = "no-store";
		return Content(token.AccessToken ?? string.Empty, "text/plain");
	}
}

namespace Shapeshifter.Website.Controllers;

using Logic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Models.Patreon.Response;

public sealed record PatreonSupporter(
	string? Id,
	string? FullName,
	double Amount,
	string? ImageUrl,
	string? Url);

[ApiController]
[Produces("application/json")]
[Route("api/patreon")]
public sealed class PatreonApiController(
	IPatreonClient client,
	IOptions<PatreonOptions> options) : ControllerBase
{
	[HttpGet("supporters")]
	public async Task<ActionResult<IEnumerable<PatreonSupporter>>> GetSupporters(CancellationToken cancellationToken)
	{
		if (!options.Value.IsConfigured)
		{
			return Problem(
				statusCode: StatusCodes.Status503ServiceUnavailable,
				detail: "The Patreon integration is not configured on this server.");
		}

		await client.EnsureAccessTokenAsync(cancellationToken);

		var pledges = await client.GetPledgesAsync(cancellationToken);
		var users = pledges.Included?.ToObject<User[]>() ?? [];

		var supporters = new List<PatreonSupporter>();
		foreach (var pledge in pledges.Data ?? [])
		{
			var patronId = pledge.Relationships?.Patron?.Data?.Id;
			var user = users.FirstOrDefault(x => x.Id == patronId);
			if (user?.Attributes is not { } attributes)
				continue;

			if (attributes.IsDeleted || attributes.IsSuspended || attributes.IsNuked)
				continue;

			supporters.Add(new PatreonSupporter(
				user.Id,
				attributes.FullName,
				(pledge.Attributes?.AmountCents ?? 0) / 100.0,
				attributes.ImageUrl,
				attributes.Url));
		}

		return supporters;
	}
}

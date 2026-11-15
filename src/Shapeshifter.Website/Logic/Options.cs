namespace Shapeshifter.Website.Logic;

/// <summary>
/// Settings for the GitHub issue reporter, bound from the <c>GitHub</c> configuration section.
/// The token must come from user-secrets (Development) or the <c>GitHub__Token</c> environment variable.
/// </summary>
public sealed class GitHubOptions
{
	public const string SectionName = "GitHub";

	public string Owner { get; set; } = "ffMathy";

	public string Repository { get; set; } = "Shapeshifter";

	public string? Token { get; set; }
}

/// <summary>
/// Settings for the Patreon integration, bound from the <c>Patreon</c> configuration section.
/// The secrets must come from user-secrets (Development) or <c>Patreon__*</c> environment variables.
/// </summary>
public sealed class PatreonOptions
{
	public const string SectionName = "Patreon";

	public int CampaignId { get; set; } = 557794;

	public string? ClientId { get; set; }

	public string? ClientSecret { get; set; }

	public string? CreatorsAccessToken { get; set; }

	public string? CreatorsRefreshToken { get; set; }

	public bool IsConfigured =>
		!string.IsNullOrWhiteSpace(ClientId) &&
		!string.IsNullOrWhiteSpace(ClientSecret) &&
		!string.IsNullOrWhiteSpace(CreatorsRefreshToken);
}

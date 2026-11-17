using System.Net.Http.Headers;
using System.Text.Json;

namespace Shapeshifter.Core.Updates;

/// <summary>A file attached to a GitHub release.</summary>
/// <param name="Sha256">The SHA-256 GitHub computed when the file was uploaded (lowercase hex), when it reports one.</param>
public sealed record ReleaseAsset(string Name, Uri DownloadUrl, long Size, string? Sha256);

/// <summary>A published GitHub release.</summary>
public sealed record ReleaseInfo(ReleaseVersion Version, string Tag, Uri Page, IReadOnlyList<ReleaseAsset> Assets)
{
    public ReleaseAsset? Asset(string name) => Assets.FirstOrDefault(asset => string.Equals(asset.Name, name, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Reads the latest release of a repository from the GitHub REST API (no account needed).</summary>
public sealed class GitHubReleases
{
    /// <summary>The checksum file attached to every release: <c>sha256sum</c> output for the other files.</summary>
    public const string ChecksumsAssetName = "SHA256SUMS.txt";

    private readonly HttpClient http;
    private readonly string owner;
    private readonly string repository;

    public GitHubReleases(HttpClient http, string owner, string repository)
    {
        this.http = http;
        this.owner = owner;
        this.repository = repository;
    }

    /// <summary>The newest release that is not a draft or a prerelease; null when the repository has none.</summary>
    public async Task<ReleaseInfo?> GetLatestAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://api.github.com/repos/{owner}/{repository}/releases/latest"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return Parse(json);
    }

    /// <summary>
    /// The expected SHA-256 of <paramref name="asset"/>: the digest GitHub reports, or else its line in the release's
    /// <see cref="ChecksumsAssetName"/>. Null when the release offers neither (the update is then refused).
    /// </summary>
    public async Task<string?> GetSha256Async(ReleaseInfo release, ReleaseAsset asset, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(release);
        ArgumentNullException.ThrowIfNull(asset);
        if (asset.Sha256 is not null)
        {
            return asset.Sha256;
        }

        if (release.Asset(ChecksumsAssetName) is not { } checksums)
        {
            return null;
        }

        var text = await http.GetStringAsync(checksums.DownloadUrl, cancellationToken).ConfigureAwait(false);
        return FindChecksum(text, asset.Name);
    }

    /// <summary>Reads a release from the API's JSON; null for drafts and anything without a version tag.</summary>
    internal static ReleaseInfo? Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || Bool(root, "draft"))
        {
            return null;
        }

        var tag = String(root, "tag_name");
        var version = ReleaseVersion.TryParse(tag);
        if (tag is null || version is null || !Uri.TryCreate(String(root, "html_url"), UriKind.Absolute, out var page))
        {
            return null;
        }

        var assets = new List<ReleaseAsset>();
        if (root.TryGetProperty("assets", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in list.EnumerateArray())
            {
                var name = String(item, "name");
                if (name is null
                    || !Uri.TryCreate(String(item, "browser_download_url"), UriKind.Absolute, out var url)
                    || url.Scheme != Uri.UriSchemeHttps)
                {
                    continue;
                }

                var size = item.TryGetProperty("size", out var sizeElement) && sizeElement.TryGetInt64(out var bytes) ? bytes : -1;
                assets.Add(new ReleaseAsset(name, url, size, Sha256FromDigest(String(item, "digest"))));
            }
        }

        return new ReleaseInfo(version, tag, page, assets);
    }

    /// <summary>The hex digest from GitHub's <c>sha256:…</c> form; null for other algorithms or malformed values.</summary>
    internal static string? Sha256FromDigest(string? digest)
    {
        const string prefix = "sha256:";
        if (digest is null || !digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return NormalizeHex(digest[prefix.Length..]);
    }

    /// <summary>Finds <paramref name="fileName"/> in <c>sha256sum</c> output (<c>hash  name</c> or <c>hash *name</c>).</summary>
    internal static string? FindChecksum(string checksums, string fileName)
    {
        foreach (var raw in checksums.Split('\n'))
        {
            var line = raw.Trim();
            var space = line.IndexOf(' ', StringComparison.Ordinal);
            if (space <= 0)
            {
                continue;
            }

            var name = line[(space + 1)..].TrimStart(' ', '*');
            if (string.Equals(name, fileName, StringComparison.Ordinal))
            {
                return NormalizeHex(line[..space]);
            }
        }

        return null;
    }

    private static string? NormalizeHex(string value)
    {
        var hex = value.Trim();
        return hex.Length == 64 && hex.All(Uri.IsHexDigit) ? hex.ToUpperInvariant() : null;
    }

    private static string? String(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool Bool(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;
}

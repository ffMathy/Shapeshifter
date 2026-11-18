using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Headers;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Localization;

namespace Shapeshifter.Core.Actions;

/// <summary>
/// Uploads a copied image to Catbox (catbox.moe), a free image host without ads, and pastes the link.
/// </summary>
public sealed class UploadImageAction : IClipboardAction
{
    /// <summary>The links of images that were already uploaded, so uploading the same item again reuses its link.</summary>
    private readonly ConcurrentDictionary<Guid, Uri> uploaded = new();

    public string Id => "upload-image";

    public int Order => 30;

    public string GetTitle(ClipboardItem item) => Strings.ActionUploadImage;

    public string GetDescription(ClipboardItem item) => Localizer.Format(Strings.ActionUploadImageDescription, Catbox.Name);

    public bool CanExecute(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Kind == ClipboardContentKind.Image && item.Snapshot.Image is { Length: > 0 };
    }

    public async Task ExecuteAsync(ClipboardItem item, ActionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(context);
        if (!uploaded.TryGetValue(item.Id, out var link))
        {
            var image = item.Snapshot.Image!;
            var name = $"Image {item.CapturedAt.LocalDateTime.ToString("yyyy-MM-dd HH.mm.ss", CultureInfo.InvariantCulture)}.{ImageFormat.GetExtension(image)}";
            link = await Catbox.UploadAsync(context.Http, image, name, cancellationToken).ConfigureAwait(false);
            uploaded[item.Id] = link;
            context.Logger.Information("Uploaded an image to {Service}", Catbox.Name);
        }

        var snapshot = new ClipboardSnapshot { Text = link.AbsoluteUri };
        await context.WriteAndPasteAsync(snapshot, cancellationToken).ConfigureAwait(false);
        context.History.Add(snapshot);
    }
}

/// <summary>Anonymous uploads to Catbox (https://catbox.moe/tools.php).</summary>
public static class Catbox
{
    public const string Name = "Catbox";

    public const long MaxBytes = 200L * 1024 * 1024;

    public static Uri Endpoint { get; } = new("https://catbox.moe/user/api.php");

    /// <summary>Uploads <paramref name="data"/> as a file called <paramref name="fileName"/> and returns its public link.</summary>
    public static async Task<Uri> UploadAsync(HttpClient http, byte[] data, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        if (data.LongLength > MaxBytes)
        {
            throw new InvalidOperationException($"The image is larger than the {MaxBytes / (1024 * 1024)} MB {Name} accepts.");
        }

        using var file = new ByteArrayContent(data);
        file.Headers.ContentType = new MediaTypeHeaderValue(ImageFormat.GetMediaType(data));
        using var form = new MultipartFormDataContent
        {
            { new StringContent("fileupload"), "reqtype" },
            { file, "fileToUpload", fileName },
        };

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        using var response = await http.PostAsync(Endpoint, form, timeout.Token).ConfigureAwait(false);
        var body = (await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false)).Trim();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"{Name} refused the upload ({(int)response.StatusCode}): {Shorten(body)}", null, response.StatusCode);
        }

        // On success the body is just the link; anything else is an error message.
        if (Uri.TryCreate(body, UriKind.Absolute, out var link) && link.Scheme == Uri.UriSchemeHttps)
        {
            return link;
        }

        throw new HttpRequestException($"{Name} did not return a link: {Shorten(body)}");
    }

    private static string Shorten(string text) => text.Length <= 200 ? text : text[..200] + "…";
}

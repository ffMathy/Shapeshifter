namespace Shapeshifter.Core.Previews;

/// <summary>Downloads an image from the web, refusing anything that is not an image or is too large.</summary>
public static class ImageDownload
{
    /// <summary>
    /// Downloads the image at <paramref name="link"/>. Fails with <see cref="HttpRequestException"/> for an
    /// unsuccessful response and with <see cref="InvalidOperationException"/> when the response is not an image or
    /// is larger than <paramref name="maxBytes"/>.
    /// </summary>
    public static async Task<byte[]> DownloadAsync(HttpClient http, Uri link, long maxBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(link);
        using var response = await http.GetAsync(link, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > maxBytes)
        {
            throw new InvalidOperationException($"The image at {link} is too large.");
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not null && !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{link} did not return an image ({mediaType}).");
        }

        // Servers may omit Content-Length, so the body is read in chunks and abandoned as soon as it is too large.
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
            {
                if (buffer.Length + read > maxBytes)
                {
                    throw new InvalidOperationException($"The image at {link} is too large.");
                }

                buffer.Write(chunk, 0, read);
            }

            return buffer.ToArray();
        }
    }
}

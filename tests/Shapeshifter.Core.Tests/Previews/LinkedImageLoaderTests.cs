using System.Net;
using System.Net.Http.Headers;
using Shapeshifter.Core.Previews;
using Shapeshifter.Core.Tests.Actions;

namespace Shapeshifter.Core.Tests.Previews;

public sealed class LinkedImageLoaderTests : IDisposable
{
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];

    private readonly FakeHttpMessageHandler handler = new();
    private readonly HttpClient http;
    private readonly LinkedImageLoader loader;
    private readonly string folder = Directory.CreateTempSubdirectory("shapeshifter-linked-").FullName;

    public LinkedImageLoaderTests()
    {
        http = new HttpClient(handler);
        loader = new LinkedImageLoader(http);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        http.Dispose();
        Directory.Delete(folder, recursive: true);
    }

    private static HttpResponseMessage Image(byte[] bytes, string? mediaType = "image/png")
    {
        var content = new ByteArrayContent(bytes);
        if (mediaType is not null)
        {
            content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        }

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    [Fact]
    public async Task Load_ALink_DownloadsTheImageOnce()
    {
        handler.Respond = _ => Image(PngBytes);
        var image = LinkedImage.ForLink(new Uri("https://example.com/cat.png"));

        Assert.False(loader.TryGetLoaded(image, out _));
        Assert.Equal(PngBytes, await loader.LoadAsync(image, Ct));
        Assert.Equal(PngBytes, await loader.LoadAsync(image, Ct));

        Assert.Equal([new Uri("https://example.com/cat.png")], handler.RequestedUris);
        Assert.True(loader.TryGetLoaded(image, out var cached));
        Assert.Equal(PngBytes, cached);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "image/png")]
    [InlineData(HttpStatusCode.OK, "text/html")]
    public async Task Load_ALinkThatIsNotAnImage_IsMissing_AndNotRequestedAgain(HttpStatusCode status, string mediaType)
    {
        handler.Respond = _ =>
        {
            var response = Image(PngBytes, mediaType);
            response.StatusCode = status;
            return response;
        };
        var image = LinkedImage.ForLink(new Uri("https://example.com/cat.png"));

        Assert.Null(await loader.LoadAsync(image, Ct));
        Assert.Null(await loader.LoadAsync(image, Ct));

        Assert.Single(handler.RequestedUris);
        Assert.True(loader.TryGetLoaded(image, out var cached));
        Assert.Null(cached);
    }

    [Fact]
    public async Task Load_ALinkToATooLargeImage_IsMissing()
    {
        handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new UnknownLengthContent(new byte[LinkedImageLoader.MaxImageBytes + 1], "image/png"),
        };

        Assert.Null(await loader.LoadAsync(LinkedImage.ForLink(new Uri("https://example.com/huge.png")), Ct));
    }

    [Fact]
    public async Task Load_AFile_ReadsIt_WithoutTheNetwork()
    {
        var path = Path.Combine(folder, "cat.png");
        await File.WriteAllBytesAsync(path, PngBytes, Ct);

        Assert.Equal(PngBytes, await loader.LoadAsync(LinkedImage.ForFile(path), Ct));
        Assert.Empty(handler.RequestedUris);
    }

    [Fact]
    public async Task Load_AMissingFile_IsMissing() =>
        Assert.Null(await loader.LoadAsync(LinkedImage.ForFile(Path.Combine(folder, "missing.png")), Ct));

    [Fact]
    public async Task Load_RemembersOnlyTheMostRecentImages()
    {
        handler.Respond = _ => Image(PngBytes);
        var first = LinkedImage.ForLink(new Uri("https://example.com/0.png"));
        for (var i = 0; i <= LinkedImageLoader.CacheCapacity; i++)
        {
            await loader.LoadAsync(LinkedImage.ForLink(new Uri($"https://example.com/{i}.png")), Ct);
        }

        Assert.False(loader.TryGetLoaded(first, out _));
        Assert.True(loader.TryGetLoaded(LinkedImage.ForLink(new Uri($"https://example.com/{LinkedImageLoader.CacheCapacity}.png")), out _));
    }
}

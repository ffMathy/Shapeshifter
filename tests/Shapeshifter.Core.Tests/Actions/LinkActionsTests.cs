using System.Net;
using System.Net.Http.Headers;
using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Tests.Actions;

public sealed class OpenLinksActionTests
{
    public static readonly TheoryData<string, bool> CanExecuteMatrix = new()
    {
        { "plain", false },
        { "whitespace", false },
        { "text+html", false },
        { "html-only", false },
        { "image", false },
        { "file", false },
        { "files", false },
        { "other", false },
        { "empty", false },
        { "link", true },
        { "image-link", true },
        { "text+image", true },
    };

    [Theory]
    [MemberData(nameof(CanExecuteMatrix))]
    public void CanExecute_OnlyForTextContainingLinks(string kind, bool expected) =>
        Assert.Equal(expected, new OpenLinksAction().CanExecute(Items.ByName(kind)));

    [Fact]
    public void CanExecute_FilesWhosePathLooksLikeALink_AreNotOffered()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Text = "https://example.com", Files = ["/tmp/x"] });

        Assert.False(new OpenLinksAction().CanExecute(item));
    }

    [Theory]
    [InlineData("go to https://example.com now", "Open link")]
    [InlineData("https://example.com and https://example.org", "Open links")]
    [InlineData("https://example.com https://example.com", "Open link")]
    [InlineData("no links", "Open links")]
    public void GetTitle_IsSingularOnlyForExactlyOneLink(string text, string expected) =>
        Assert.Equal(expected, new OpenLinksAction().GetTitle(Items.LinkText(text)));

    [Fact]
    public async Task Execute_OpensEveryDistinctLinkInOrder()
    {
        using var harness = new ActionTestHarness();
        var item = Items.LinkText("a https://example.com/1, (www.example.org) and https://example.com/1 again http://localhost/x");

        await new OpenLinksAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        Assert.Equal(
            [new Uri("https://example.com/1"), new Uri("https://www.example.org"), new Uri("http://localhost/x")],
            harness.Launcher.OpenedUrls);
        Assert.Empty(harness.Backend.Writes);
        Assert.Equal(0, harness.Paste.Calls);
        Assert.Equal(0, harness.History.Count);
    }

    [Fact]
    public async Task Execute_OpensAtMostMaxLinks()
    {
        using var harness = new ActionTestHarness();
        var links = Enumerable.Range(1, OpenLinksAction.MaxLinks + 5).Select(i => $"https://example.com/{i}").ToArray();
        var item = Items.LinkText(string.Join(Environment.NewLine, links));

        await new OpenLinksAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        Assert.Equal(10, OpenLinksAction.MaxLinks);
        Assert.Equal(links.Take(OpenLinksAction.MaxLinks).Select(link => new Uri(link)), harness.Launcher.OpenedUrls);
    }

    [Fact]
    public async Task Execute_ExactlyMaxLinks_OpensAll()
    {
        using var harness = new ActionTestHarness();
        var links = Enumerable.Range(1, OpenLinksAction.MaxLinks).Select(i => $"https://example.com/{i}");
        var item = Items.LinkText(string.Join(' ', links));

        await new OpenLinksAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        Assert.Equal(OpenLinksAction.MaxLinks, harness.Launcher.OpenedUrls.Count);
    }
}

public sealed class CopyImageFromLinkActionTests
{
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];

    public static readonly TheoryData<string, bool> CanExecuteMatrix = new()
    {
        { "plain", false },
        { "text+html", false },
        { "html-only", false },
        { "image", false },
        { "file", false },
        { "files", false },
        { "other", false },
        { "empty", false },
        { "link", false },
        { "image-link", true },
        { "text+image", true },
    };

    [Theory]
    [MemberData(nameof(CanExecuteMatrix))]
    public void CanExecute_OnlyForTextContainingAnImageLink(string kind, bool expected) =>
        Assert.Equal(expected, new CopyImageFromLinkAction().CanExecute(Items.ByName(kind)));

    [Theory]
    [InlineData("https://example.com/a.PNG", true)]
    [InlineData("https://example.com/a.jpeg?size=large", true)]
    [InlineData("https://example.com/a.webp#frag", true)]
    [InlineData("https://example.com/a.svg", false)]
    [InlineData("https://example.com/png", false)]
    public void CanExecute_DependsOnTheLinkPathExtension(string text, bool expected) =>
        Assert.Equal(expected, new CopyImageFromLinkAction().CanExecute(Items.LinkText(text)));

    [Fact]
    public async Task Execute_DownloadsFirstImageLink_AndAddsAnImageItem()
    {
        using var harness = new ActionTestHarness();
        harness.Handler.Respond = _ => ImageResponse(PngBytes, "image/png");
        var existing = harness.AddToHistory(new ClipboardSnapshot
        {
            Text = "page https://example.com/page then https://example.com/pic.png and https://example.com/b.jpg",
        });

        await new CopyImageFromLinkAction().ExecuteAsync(existing, harness.Context, ActionTestHarness.Ct);

        Assert.Equal([new Uri("https://example.com/pic.png")], harness.Handler.RequestedUris);
        var written = Assert.Single(harness.Backend.Writes);
        Assert.Equal(PngBytes, written.Image);
        Assert.Null(written.Text);
        Assert.Empty(written.Files);
        Assert.Empty(written.RawFormats);

        var top = harness.History.Items[0];
        Assert.Equal(ClipboardContentKind.Image, top.Kind);
        Assert.Equal(PngBytes, top.Snapshot.Image);
        Assert.Equal(existing.Id, harness.History.Items[1].Id);
        Assert.Equal(0, harness.Paste.Calls);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("IMAGE/PNG")]
    [InlineData(null)]
    public async Task Execute_AcceptsImageOrMissingContentType(string? mediaType)
    {
        using var harness = new ActionTestHarness();
        harness.Handler.Respond = _ => ImageResponse(PngBytes, mediaType);

        await new CopyImageFromLinkAction().ExecuteAsync(Items.ByName("image-link"), harness.Context, ActionTestHarness.Ct);

        Assert.Equal(PngBytes, Assert.Single(harness.Backend.Writes).Image);
        Assert.Equal(1, harness.History.Count);
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("application/octet-stream")]
    public async Task Execute_RejectsNonImageContent(string mediaType)
    {
        using var harness = new ActionTestHarness();
        harness.Handler.Respond = _ => ImageResponse(Items.Utf8("<html></html>"), mediaType);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new CopyImageFromLinkAction().ExecuteAsync(Items.ByName("image-link"), harness.Context, ActionTestHarness.Ct));

        Assert.Contains(mediaType, exception.Message, StringComparison.Ordinal);
        Assert.Empty(harness.Backend.Writes);
        Assert.Equal(0, harness.History.Count);
    }

    [Fact]
    public async Task Execute_RejectsDeclaredOversizeContent()
    {
        using var harness = new ActionTestHarness();
        harness.Handler.Respond = _ =>
        {
            var response = ImageResponse(PngBytes, "image/png");
            response.Content.Headers.ContentLength = CopyImageFromLinkAction.MaxImageBytes + 1;
            return response;
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new CopyImageFromLinkAction().ExecuteAsync(Items.ByName("image-link"), harness.Context, ActionTestHarness.Ct));

        Assert.Contains("too large", exception.Message, StringComparison.Ordinal);
        Assert.Empty(harness.Backend.Writes);
        Assert.Equal(0, harness.History.Count);
    }

    [Fact]
    public async Task Execute_AcceptsContentExactlyAtTheLimitDeclaration()
    {
        using var harness = new ActionTestHarness();
        harness.Handler.Respond = _ =>
        {
            var response = ImageResponse(PngBytes, "image/png");
            response.Content.Headers.ContentLength = PngBytes.Length;
            return response;
        };

        await new CopyImageFromLinkAction().ExecuteAsync(Items.ByName("image-link"), harness.Context, ActionTestHarness.Ct);

        Assert.Single(harness.Backend.Writes);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Execute_FailingStatus_Throws_AndCopiesNothing(HttpStatusCode status)
    {
        using var harness = new ActionTestHarness();
        harness.Handler.Respond = _ => new HttpResponseMessage(status);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => new CopyImageFromLinkAction().ExecuteAsync(Items.ByName("image-link"), harness.Context, ActionTestHarness.Ct));

        Assert.Empty(harness.Backend.Writes);
        Assert.Equal(0, harness.History.Count);
    }

    /// <summary>
    /// <see cref="CopyImageFromLinkAction.MaxImageBytes"/> must also hold when the server does not announce a
    /// Content-Length (chunked transfer); otherwise an arbitrarily large body is buffered and copied.
    /// </summary>
    [Fact]
    public async Task Execute_RejectsOversizeContentWithoutContentLength()
    {
        using var harness = new ActionTestHarness();
        var huge = new byte[CopyImageFromLinkAction.MaxImageBytes + 1];
        harness.Handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new UnknownLengthContent(huge, "image/png"),
        };

        await Assert.ThrowsAnyAsync<Exception>(
            () => new CopyImageFromLinkAction().ExecuteAsync(Items.ByName("image-link"), harness.Context, ActionTestHarness.Ct));

        Assert.Empty(harness.Backend.Writes);
        Assert.Equal(0, harness.History.Count);
    }

    private static HttpResponseMessage ImageResponse(byte[] bytes, string? mediaType)
    {
        var content = new ByteArrayContent(bytes);
        if (mediaType is not null)
        {
            content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        }

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }
}

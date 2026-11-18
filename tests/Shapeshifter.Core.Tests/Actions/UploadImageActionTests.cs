using System.Net;
using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Tests.Actions;

public sealed class UploadImageActionTests
{
    private const string Link = "https://files.catbox.moe/abc123.png";

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

    public static readonly TheoryData<string, bool> CanExecuteMatrix = new()
    {
        { "image", true },
        { "plain", false },
        { "text+image", false },
        { "image-link", false },
        { "file", false },
        { "files", false },
        { "other", false },
        { "empty", false },
    };

    [Theory]
    [MemberData(nameof(CanExecuteMatrix))]
    public void CanExecute_OnlyForImages(string kind, bool expected) =>
        Assert.Equal(expected, new UploadImageAction().CanExecute(Items.ByName(kind)));

    [Fact]
    public void CanExecute_IsFalseForAnEmptyImage() =>
        Assert.False(new UploadImageAction().CanExecute(new ClipboardItem(new ClipboardSnapshot { Image = [] })));

    [Fact]
    public void Description_NamesCatbox() =>
        Assert.Contains("Catbox", new UploadImageAction().GetDescription(Items.ByName("image")), StringComparison.Ordinal);

    [Fact]
    public async Task Execute_UploadsTheImageToCatbox_AndPastesTheLink()
    {
        using var harness = new ActionTestHarness();
        string? body = null;
        harness.Handler.Respond = request =>
        {
            body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Link + "\n") };
        };
        var item = harness.AddToHistory(new ClipboardSnapshot { Image = Png });

        await new UploadImageAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        Assert.Equal(Catbox.Endpoint, Assert.Single(harness.Handler.RequestedUris));
        Assert.Contains("name=reqtype", body, StringComparison.Ordinal);
        Assert.Contains("fileupload", body, StringComparison.Ordinal);
        Assert.Contains("name=fileToUpload", body, StringComparison.Ordinal);
        Assert.Contains(".png", body, StringComparison.Ordinal);
        Assert.Contains("image/png", body, StringComparison.Ordinal);

        var written = Assert.Single(harness.Backend.Writes);
        Assert.Equal(Link, written.Text);
        Assert.Equal(1, harness.Paste.Calls);
        Assert.Equal(Link, harness.History.Items[0].Snapshot.Text);
    }

    [Fact]
    public async Task Execute_TwiceForTheSameItem_UploadsOnce()
    {
        using var harness = new ActionTestHarness();
        harness.Handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Link) };
        var item = harness.AddToHistory(new ClipboardSnapshot { Image = Png });
        var action = new UploadImageAction();

        await action.ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);
        await action.ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        Assert.Single(harness.Handler.RequestedUris);
        Assert.Equal(2, harness.Paste.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "Error: something went wrong")]
    [InlineData(HttpStatusCode.OK, "http://files.catbox.moe/abc.png")]
    [InlineData(HttpStatusCode.OK, "")]
    [InlineData(HttpStatusCode.PreconditionFailed, "File too large")]
    [InlineData(HttpStatusCode.InternalServerError, Link)]
    public async Task Execute_WhenCatboxReturnsNoLink_FailsAndPastesNothing(HttpStatusCode status, string response)
    {
        using var harness = new ActionTestHarness();
        harness.Handler.Respond = _ => new HttpResponseMessage(status) { Content = new StringContent(response) };
        var item = harness.AddToHistory(new ClipboardSnapshot { Image = Png });

        await Assert.ThrowsAsync<HttpRequestException>(() => new UploadImageAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct));

        Assert.Empty(harness.Backend.Writes);
        Assert.Equal(0, harness.Paste.Calls);
    }
}

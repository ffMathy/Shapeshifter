using System.Net;
using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Settings;

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
    public async Task Execute_WithoutConsent_UploadsNothing()
    {
        using var harness = new ActionTestHarness();
        harness.Consent.Result = UploadConsentResult.Denied;
        var item = harness.AddToHistory(new ClipboardSnapshot { Image = Png });

        await new UploadImageAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        Assert.Equal(["Catbox"], harness.Consent.Requests);
        Assert.Empty(harness.Handler.RequestedUris);
        Assert.Empty(harness.Backend.Writes);
        Assert.Equal(0, harness.Paste.Calls);
    }

    [Fact]
    public async Task Execute_ByDefault_NeverUploads()
    {
        using var harness = new ActionTestHarness();
        var context = new ActionContext
        {
            Clipboard = harness.Monitor,
            Paste = harness.Paste,
            History = harness.History,
            PinnedItems = harness.PinnedItems,
            Launcher = harness.Launcher,
            Paths = harness.Paths,
            Http = harness.Http,
        };
        var item = harness.AddToHistory(new ClipboardSnapshot { Image = Png });

        await new UploadImageAction().ExecuteAsync(item, context, ActionTestHarness.Ct);

        Assert.Empty(harness.Handler.RequestedUris);
    }

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
    public async Task Execute_RightAfterTheUserAgreed_CopiesTheLinkInsteadOfPasting()
    {
        using var harness = new ActionTestHarness();
        harness.Consent.Result = UploadConsentResult.GrantedNow;
        harness.Handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Link) };
        var item = harness.AddToHistory(new ClipboardSnapshot { Image = Png });

        await new UploadImageAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        Assert.Equal(Link, Assert.Single(harness.Backend.Writes).Text);
        Assert.Equal(0, harness.Paste.Calls);
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

public sealed class RememberedUploadConsentTests : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("shapeshifter-consent-").FullName;

    private string SettingsPath => Path.Combine(directory, "settings.json");

    public void Dispose()
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public async Task AsksTheFirstTime_AndRemembersAYes()
    {
        var settings = new SettingsStore(SettingsPath);
        var asked = 0;
        using var consent = new RememberedUploadConsent(settings, (_, _) => { asked++; return Task.FromResult(true); });

        Assert.Equal(UploadConsentResult.GrantedNow, await consent.RequestAsync("Catbox", TestContext.Current.CancellationToken));
        Assert.Equal(UploadConsentResult.GrantedEarlier, await consent.RequestAsync("Catbox", TestContext.Current.CancellationToken));
        Assert.Equal(1, asked);

        // Remembered across restarts.
        using var reloaded = new RememberedUploadConsent(new SettingsStore(SettingsPath), (_, _) => throw new InvalidOperationException("Asked again"));
        Assert.Equal(UploadConsentResult.GrantedEarlier, await reloaded.RequestAsync("Catbox", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DoesNotRememberANo()
    {
        var settings = new SettingsStore(SettingsPath);
        var asked = 0;
        using var consent = new RememberedUploadConsent(settings, (_, _) => { asked++; return Task.FromResult(false); });

        Assert.Equal(UploadConsentResult.Denied, await consent.RequestAsync("Catbox", TestContext.Current.CancellationToken));
        Assert.Equal(UploadConsentResult.Denied, await consent.RequestAsync("Catbox", TestContext.Current.CancellationToken));
        Assert.Equal(2, asked);
        Assert.Empty(settings.Current.UploadConsents);
    }

    [Fact]
    public async Task ConsentIsPerService()
    {
        var settings = new SettingsStore(SettingsPath);
        var asked = new List<string>();
        using var consent = new RememberedUploadConsent(settings, (service, _) => { asked.Add(service); return Task.FromResult(true); });

        await consent.RequestAsync("Catbox", TestContext.Current.CancellationToken);
        await consent.RequestAsync("Elsewhere", TestContext.Current.CancellationToken);

        Assert.Equal(["Catbox", "Elsewhere"], asked);
    }

    [Fact]
    public async Task NoUploadConsent_AlwaysDenies() =>
        Assert.Equal(UploadConsentResult.Denied, await NoUploadConsent.Instance.RequestAsync("Catbox", TestContext.Current.CancellationToken));
}

public sealed class UploadConsentSettingsTests
{
    [Fact]
    public void SettingsWithTheSameConsents_AreEqual() =>
        Assert.Equal(new ShapeshifterSettings { UploadConsents = ["Catbox"] }, new ShapeshifterSettings { UploadConsents = ["Catbox"] });

    /// <summary>
    /// <see cref="ShapeshifterSettings.Equals(ShapeshifterSettings?)"/> lists the properties by hand. A property it
    /// forgets would make <see cref="SettingsStore"/> drop changes to it, so changing any property must make a difference.
    /// </summary>
    [Fact]
    public void ChangingAnyProperty_MakesSettingsDiffer()
    {
        var original = new ShapeshifterSettings();
        foreach (var property in typeof(ShapeshifterSettings).GetProperties().Where(p => p.CanWrite))
        {
            var changed = original with { };
            object value = property.PropertyType switch
            {
                var t when t == typeof(int) => (int)property.GetValue(original)! + 1,
                var t when t == typeof(bool) => !(bool)property.GetValue(original)!,
                var t when t == typeof(IReadOnlyList<string>) => new[] { "changed" },
                var t when t == typeof(PasteShortcut) => PasteShortcut.WindowsV,
                var t => throw new InvalidOperationException($"Add a changed value for {t.Name} ({property.Name}) to this test."),
            };
            property.SetValue(changed, value);

            Assert.False(original.Equals(changed), $"{property.Name} is not compared in ShapeshifterSettings.Equals.");
        }
    }

    [Fact]
    public void SettingsWithDifferentConsents_DifferFromEachOther() =>
        Assert.NotEqual(new ShapeshifterSettings(), new ShapeshifterSettings { UploadConsents = ["Catbox"] });
}

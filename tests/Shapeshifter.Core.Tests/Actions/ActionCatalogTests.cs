using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Tests.Actions;

public sealed class ActionCatalogTests
{
    private static readonly string[] DefaultOrder =
    [
        "paste", "paste-as-file", "paste-plain-text", "upload-image", "copy-as-zip", "copy-image-from-link", "open-links", "toggle-pin",
    ];

    [Fact]
    public void CreateDefault_ListsAllActionsInDisplayOrder()
    {
        var catalog = ActionCatalog.CreateDefault();

        Assert.Equal(DefaultOrder, catalog.All.Select(action => action.Id));
        Assert.Equal([0, 10, 20, 30, 75, 100, 200, 255], catalog.All.Select(action => action.Order));
    }

    [Fact]
    public void CreateDefault_IdsAreUnique()
    {
        var ids = ActionCatalog.CreateDefault().All.Select(action => action.Id).ToArray();

        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Constructor_SortsByOrder_AndKeepsInputOrderForTies()
    {
        var late = new StubAction("late", 50);
        var tieA = new StubAction("tie-a", 5);
        var early = new StubAction("early", -1);
        var tieB = new StubAction("tie-b", 5);

        var catalog = new ActionCatalog([late, tieA, early, tieB]);

        Assert.Equal(["early", "tie-a", "tie-b", "late"], catalog.All.Select(action => action.Id));
    }

    [Fact]
    public void Constructor_SnapshotsTheSequence()
    {
        var source = new List<IClipboardAction> { new StubAction("a", 1) };
        var catalog = new ActionCatalog(source);

        source.Add(new StubAction("b", 0));

        Assert.Equal(["a"], catalog.All.Select(action => action.Id));
    }

    [Fact]
    public void Constructor_RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => new ActionCatalog(null!));

    [Fact]
    public void GetAvailable_FiltersByCanExecute_PreservingOrder()
    {
        var catalog = new ActionCatalog(
        [
            new StubAction("c", 3, canExecute: true),
            new StubAction("a", 1, canExecute: true),
            new StubAction("b", 2, canExecute: false),
        ]);

        var available = catalog.GetAvailable(Items.ByName("plain"));

        Assert.Equal(["a", "c"], available.Select(action => action.Id));
    }

    [Fact]
    public void GetAvailable_IsEmptyWhenNothingApplies()
    {
        var catalog = new ActionCatalog([new StubAction("x", 0, canExecute: false)]);

        Assert.Empty(catalog.GetAvailable(Items.ByName("plain")));
    }

    public static readonly TheoryData<string, string[]> DefaultAvailability = new()
    {
        { "plain", ["paste", "paste-as-file", "toggle-pin"] },
        { "whitespace", ["paste", "paste-as-file", "toggle-pin"] },
        { "text+html", ["paste", "paste-as-file", "paste-plain-text", "toggle-pin"] },
        { "html-only", ["paste", "paste-plain-text", "toggle-pin"] },
        { "empty-text+html", ["paste", "paste-plain-text", "toggle-pin"] },
        { "rtf-only", ["paste", "toggle-pin"] },  // RTF without text has no plain text to offer
        { "image", ["paste", "paste-as-file", "upload-image", "toggle-pin"] },
        { "file", ["paste", "copy-as-zip", "toggle-pin"] },
        { "files", ["paste", "copy-as-zip", "toggle-pin"] },
        { "file+html", ["paste", "copy-as-zip", "toggle-pin"] },
        { "other", ["paste", "toggle-pin"] },
        { "empty", ["paste", "toggle-pin"] },
        { "link", ["paste", "paste-as-file", "open-links", "toggle-pin"] },
        { "image-link", ["paste", "paste-as-file", "copy-image-from-link", "open-links", "toggle-pin"] },
        { "text+image", ["paste", "paste-as-file", "copy-image-from-link", "open-links", "toggle-pin"] },
    };

    [Theory]
    [MemberData(nameof(DefaultAvailability))]
    public void GetAvailable_DefaultCatalog_MatchesTheCanExecuteMatrix(string kind, string[] expectedIds)
    {
        var available = ActionCatalog.CreateDefault().GetAvailable(Items.ByName(kind));

        Assert.Equal(expectedIds, available.Select(action => action.Id));
    }

    [Fact]
    public void DefaultAvailability_CoversEveryRepresentativeKind()
    {
        var covered = DefaultAvailability.Select(row => (string)row.Data.Item1).ToHashSet();

        Assert.All(Items.AllKinds.Select(row => row.Data), kind => Assert.Contains(kind, covered));
    }

    [Theory]
    [MemberData(nameof(Items.AllKinds), MemberType = typeof(Items))]
    public void FirstAvailableAction_IsAlwaysPaste(string kind)
    {
        var available = ActionCatalog.CreateDefault().GetAvailable(Items.ByName(kind));

        Assert.Equal("paste", available[0].Id);
        Assert.Equal("toggle-pin", available[^1].Id);
    }

    [Fact]
    public void CanExecute_RejectsNullItem_ForItemSensitiveActions()
    {
        IClipboardAction[] actions =
        [
            new PasteAsPlainTextAction(), new PasteAsFileAction(), new ZipFilesAction(), new CopyImageFromLinkAction(),
            new OpenLinksAction(), new UploadImageAction(),
        ];

        Assert.All(actions, action => Assert.Throws<ArgumentNullException>(() => action.CanExecute(null!)));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsNullArguments()
    {
        using var harness = new ActionTestHarness();
        var item = Items.ByName("plain");

        foreach (var action in ActionCatalog.CreateDefault().All)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => action.ExecuteAsync(null!, harness.Context, ActionTestHarness.Ct));
            await Assert.ThrowsAsync<ArgumentNullException>(() => action.ExecuteAsync(item, null!, ActionTestHarness.Ct));
        }
    }

    [Fact]
    public void Titles_AreStable()
    {
        var item = Items.ByName("plain");

        Assert.Equal("Paste", new PasteAction().GetTitle(item));
        Assert.Equal("Paste as plain text", new PasteAsPlainTextAction().GetTitle(item));
        Assert.Equal("Paste as file", new PasteAsFileAction().GetTitle(item));
        Assert.Equal("Copy as compressed folder", new ZipFilesAction().GetTitle(item));
        Assert.Equal("Copy image from link", new CopyImageFromLinkAction().GetTitle(item));
        Assert.Equal("Upload and paste link", new UploadImageAction().GetTitle(item));
    }

    [Theory]
    [InlineData("plain")]
    [InlineData("link")]
    [InlineData("text+html")]
    [InlineData("text+image")]
    [InlineData("file")]
    [InlineData("files")]
    [InlineData("image")]
    public void Descriptions_AreSentences_ForEveryAvailableAction(string kind)
    {
        var item = Items.ByName(kind);

        Assert.All(ActionCatalog.CreateDefault().GetAvailable(item), action =>
        {
            var description = action.GetDescription(item);
            Assert.False(string.IsNullOrWhiteSpace(description));
            Assert.EndsWith(".", description, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Descriptions_FollowPinState()
    {
        var item = Items.ByName("plain");
        var action = new TogglePinAction();

        var unpinned = action.GetDescription(item);
        item.IsPinned = true;

        Assert.NotEqual(unpinned, action.GetDescription(item));
    }

    private sealed class StubAction(string id, int order, bool canExecute = true) : IClipboardAction
    {
        public string Id => id;

        public int Order => order;

        public string GetTitle(ClipboardItem item) => id;

        public string GetDescription(ClipboardItem item) => id;

        public bool CanExecute(ClipboardItem item) => canExecute;

        public Task ExecuteAsync(ClipboardItem item, ActionContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}

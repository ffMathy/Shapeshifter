using NSubstitute;
using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Coaching;
using Shapeshifter.Core.Engine;
using Shapeshifter.Core.Input;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Tests.Engine;

public sealed class ShapeshifterEngineTests : IAsyncLifetime
{
    private static readonly string[] DefaultTextActions = ["Paste", "Paste as file", "Pin to clipboard"];

    private EngineHarness h = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => h.DisposeAsync();

    private async Task UseCatalogAsync(Func<CallLog, ActionCatalog> catalog)
    {
        await h.DisposeAsync();
        h = new EngineHarness(catalog);
    }

    private static Guid[] Ids(IEnumerable<ClipboardItem> items) => items.Select(item => item.Id).ToArray();

    private static string?[] Texts(IEnumerable<ClipboardItem> items) => items.Select(item => item.Snapshot.Text).ToArray();

    // ---------------------------------------------------------------- start-up

    [Fact]
    public async Task StartAsync_InstallsKeyHandlerBeforeStartingTheHook()
    {
        Assert.Null(h.Hook.KeyHandler);

        await h.StartAsync();

        Assert.NotNull(h.Hook.KeyHandler);
        Assert.Equal(1, h.Hook.StartCount);
        Assert.True(h.Hook.HandlerSetBeforeStart);
        Assert.Equal(1, h.ChangeSource.StartCount);
    }

    [Fact]
    public async Task StartAsync_CapturesTheCurrentClipboard_AndNotifiesForIt()
    {
        h.Backend.Current = EngineHarness.Text("hello");

        await h.StartAsync();

        var item = Assert.Single(h.History.Items);
        Assert.Equal("hello", item.Snapshot.Text);
        var notification = Assert.Single(h.Notifier.Calls);
        Assert.Same(item, notification.Item);
        Assert.Equal(1, notification.HistoryCount);
    }

    [Fact]
    public async Task StartAsync_WithEmptyClipboard_CapturesNothing()
    {
        await h.StartAsync();

        Assert.Empty(h.History.Items);
        Assert.Empty(h.Notifier.Calls);
    }

    [Fact]
    public async Task StartAsync_ClipboardAlreadyInHistory_DoesNotNotify()
    {
        h.Seed("a", "b");
        h.Backend.Current = EngineHarness.Text("a");

        await h.StartAsync();

        Assert.Empty(h.Notifier.Calls);
        Assert.Equal(["a", "b"], Texts(h.History.Items));
    }

    // ---------------------------------------------------------------- copy notifications

    [Fact]
    public async Task CopyNotifier_IsCalledForNewCapturesOnly_WithTheHistoryCount()
    {
        await h.StartAsync();

        h.Backend.Current = EngineHarness.Text("a");
        await h.RaiseClipboardChangeAndWaitAsync();
        h.Backend.Current = EngineHarness.Text("b");
        await h.RaiseClipboardChangeAndWaitAsync();
        h.Backend.Current = EngineHarness.Text("a");
        await h.RaiseClipboardChangeAndWaitAsync();

        var calls = h.Notifier.Calls;
        Assert.Equal(2, calls.Count);
        Assert.Equal("a", calls[0].Item.Snapshot.Text);
        Assert.Equal(1, calls[0].HistoryCount);
        Assert.Equal("b", calls[1].Item.Snapshot.Text);
        Assert.Equal(2, calls[1].HistoryCount);

        // The duplicate was moved to the top but did not notify.
        Assert.Equal(["a", "b"], Texts(h.History.Items));
    }

    [Fact]
    public async Task CopyNotifier_IsNotCalledForTheEnginesOwnClipboardWrite()
    {
        h.Seed("a", "b", "c");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        h.Up(GestureKey.Modifier);
        await h.WaitForEffectsAsync(3);

        // The platform reports the change Shapeshifter just made.
        await h.RaiseClipboardChangeAndWaitAsync();

        Assert.Empty(h.Notifier.Calls);
        Assert.Equal(["b", "c", "a"], Texts(h.History.Items));
    }

    // ---------------------------------------------------------------- searching (issue #458)

    /// <summary>Holds the modifier, presses V and then S, and releases: the search begins.</summary>
    private async Task BeginSearchAsync()
    {
        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        Assert.True(h.Tap(GestureKey.Search));
        h.Up(GestureKey.Modifier);
        await EngineHarness.WaitUntilAsync(() => h.Picker.SearchInput is not null, "the search began");
    }

    private Task<IReadOnlyList<GestureEffect>> WaitForEffectAsync<T>()
        where T : GestureEffect => WaitForAsync(() => h.Processed.OfType<T>().Any(), typeof(T).Name);

    private async Task<IReadOnlyList<GestureEffect>> WaitForAsync(Func<bool> condition, string description)
    {
        await EngineHarness.WaitUntilAsync(condition, description);
        return h.Processed;
    }

    [Fact]
    public async Task Search_S_KeepsThePickerOpenForSearching_WithEveryItem()
    {
        var seeded = h.Seed("apple", "banana", "cherry");
        await h.StartAsync();

        await BeginSearchAsync();

        // S before the show delay shows the picker first.
        var calls = h.Picker.Calls;
        Assert.Equal(["Show", "BeginSearch"], calls.Select(call => call.Method));
        var begun = calls[^1];
        Assert.Equal("BeginSearch", begun.Method);
        Assert.Equal(Ids(seeded), Ids(begun.State!.Items));
        Assert.Equal(string.Empty, begun.State.SearchQuery);
        Assert.Equal(0, begun.State.SelectedIndex);
        Assert.Equal(DefaultTextActions, begun.State.Actions.Select(action => action.Title));
        Assert.DoesNotContain(calls, call => call.Method == "Hide");

        // The focus was noted before the picker took it, and nothing was pasted.
        Assert.Equal(1, h.Log.Entries.Count(entry => entry == "focus.capture"));
        Assert.True(h.Log.Entries.ToList().IndexOf("focus.capture") < h.Log.Entries.ToList().IndexOf("picker.BeginSearch"));
        Assert.Equal(0, h.Paste.Count);
        Assert.True(h.Engine.IsSearching);
        Assert.False(h.Engine.IsGestureActive);
    }

    [Fact]
    public async Task Search_TypingFiltersTheItems()
    {
        var seeded = h.Seed("apple pie", "banana", "apple juice");
        await h.StartAsync();
        await BeginSearchAsync();

        Assert.True(h.Picker.Search(PickerSearchInput.Typed("APPLE")));
        await WaitForAsync(() => h.Picker.Calls[^1].State?.SearchQuery == "APPLE", "the filtered picker");

        var state = h.Picker.Calls[^1].State!;
        Assert.Equal("Update", h.Picker.Calls[^1].Method);
        Assert.Equal([seeded[0].Id, seeded[2].Id], Ids(state.Items));
        Assert.Equal(0, state.SelectedIndex);

        h.Picker.Search(PickerSearchInput.Typed("kiwi"));
        await WaitForAsync(() => h.Picker.Calls[^1].State?.SearchQuery == "kiwi", "the empty result");
        Assert.Empty(h.Picker.Calls[^1].State!.Items);
        Assert.Empty(h.Picker.Calls[^1].State!.Actions);
    }

    [Fact]
    public async Task Search_Enter_RestoresTheFocus_HidesThePicker_ThenPastesTheMatch()
    {
        h.Seed("apple pie", "banana", "cherry");
        await h.StartAsync();
        await BeginSearchAsync();

        h.Picker.Search(PickerSearchInput.Typed("pie"));
        Assert.True(h.Picker.Search(new PickerSearchInput(PickerSearchKey.Enter)));
        await WaitForEffectAsync<PerformAction>();

        Assert.Equal("apple pie", Assert.Single(h.Paste.ClipboardAtPaste).Text);
        var log = h.Log.Entries.SkipWhile(entry => entry != "picker.BeginSearch").Where(entry => entry != "picker.Update").ToArray();
        Assert.Equal(["picker.BeginSearch", "focus.restore:42", "picker.Hide", "clipboard.write:apple pie", "paste"], log);
        Assert.False(h.Engine.IsSearching);
        Assert.Equal("apple pie", h.History.Items[0].Snapshot.Text);
    }

    [Fact]
    public async Task Search_ChoosingAnotherAction_PerformsThatAction()
    {
        h.Seed("apple", "banana");
        await h.StartAsync();
        await BeginSearchAsync();

        h.Picker.Search(PickerSearchInput.Typed("banana"));
        h.Picker.Search(new PickerSearchInput(PickerSearchKey.Right));
        h.Picker.Search(new PickerSearchInput(PickerSearchKey.Down));
        h.Picker.Search(new PickerSearchInput(PickerSearchKey.Down));
        h.Picker.Search(new PickerSearchInput(PickerSearchKey.Enter));
        await WaitForEffectAsync<PerformAction>();

        // "Pin to clipboard" pins rather than pastes.
        Assert.Equal(0, h.Paste.Count);
        Assert.True(h.History.Items.Single(item => item.Snapshot.Text == "banana").IsPinned);
    }

    [Fact]
    public async Task Search_Escape_RestoresTheFocus_AndPastesNothing()
    {
        h.Seed("apple", "banana");
        await h.StartAsync();
        await BeginSearchAsync();

        h.Picker.Search(new PickerSearchInput(PickerSearchKey.Escape));
        await WaitForEffectAsync<GestureCancelled>();

        Assert.Equal([new FocusedWindow(42)], h.FocusTracker.Restored);
        Assert.Equal("Hide", h.Picker.Calls[^1].Method);
        Assert.Equal(0, h.Paste.Count);
        Assert.Empty(h.Backend.Written);
        Assert.False(h.Engine.IsSearching);
    }

    [Fact]
    public async Task Search_LosingTheFocus_HidesThePicker_WithoutTakingTheFocusBack()
    {
        h.Seed("apple", "banana");
        await h.StartAsync();
        await BeginSearchAsync();

        h.Picker.Search(new PickerSearchInput(PickerSearchKey.Deactivated));
        await WaitForEffectAsync<GestureCancelled>();

        Assert.Empty(h.FocusTracker.Restored);
        Assert.Equal("Hide", h.Picker.Calls[^1].Method);
        Assert.Equal(0, h.Paste.Count);
    }

    [Fact]
    public async Task Search_WithoutAFocusedWindow_StillPastes()
    {
        h.Seed("apple", "banana");
        h.FocusTracker.Focused = null;
        await h.StartAsync();
        await BeginSearchAsync();

        h.Picker.Search(new PickerSearchInput(PickerSearchKey.Enter));
        await WaitForEffectAsync<PerformAction>();

        Assert.Empty(h.FocusTracker.Restored);
        Assert.Equal(1, h.Paste.Count);
    }

    [Fact]
    public async Task Search_KeysGoToThePicker_NotToTheGesture()
    {
        h.Seed("apple", "banana");
        await h.StartAsync();
        await BeginSearchAsync();
        var calls = h.Picker.Calls.Count;

        // Ctrl+V into the search box, S, the arrows and Escape are the search box's, not a new gesture's.
        Assert.False(h.Down(GestureKey.Modifier));
        Assert.False(h.Tap(GestureKey.Trigger));
        Assert.False(h.Tap(GestureKey.Search));
        Assert.False(h.Tap(GestureKey.Down));
        Assert.False(h.Tap(GestureKey.Escape));
        Assert.False(h.Up(GestureKey.Modifier));
        await Task.Delay(100, Ct);

        Assert.False(h.Engine.IsGestureActive);
        Assert.Equal(calls, h.Picker.Calls.Count);
        Assert.True(h.Engine.IsSearching);
    }

    [Fact]
    public async Task Search_AfterItEnds_TheGestureWorksAgain_WithTheModifierStillTracked()
    {
        h.Seed("apple", "banana");
        await h.StartAsync();
        await BeginSearchAsync();

        // The modifier goes down while the search box has the focus, and stays down after the search ends.
        h.Down(GestureKey.Modifier);
        h.Picker.Search(new PickerSearchInput(PickerSearchKey.Escape));
        await WaitForEffectAsync<GestureCancelled>();

        Assert.True(h.Down(GestureKey.Trigger));
        h.Up(GestureKey.Trigger);
        Assert.True(h.Engine.IsGestureActive);
        h.Up(GestureKey.Modifier);
        await WaitForEffectAsync<PasteCurrentClipboard>();
        Assert.Equal(1, h.Paste.Count);
    }

    [Fact]
    public async Task Search_BegunWhileVIsHeld_TheNextCtrlVStillWorks()
    {
        h.Hook.GestureModifierPastes = true;
        h.Seed("apple", "banana");
        await h.StartAsync();

        // Ctrl and V held, S searches; V and Ctrl come up while the search box has the focus.
        h.Down(GestureKey.Modifier);
        h.Down(GestureKey.Trigger);
        Assert.True(h.Tap(GestureKey.Search));
        Assert.False(h.Up(GestureKey.Trigger));
        h.Up(GestureKey.Modifier);
        await EngineHarness.WaitUntilAsync(() => h.Picker.SearchInput is not null, "the search began");
        h.Picker.Search(new PickerSearchInput(PickerSearchKey.Escape));
        await WaitForEffectAsync<GestureCancelled>();

        // The next quick Ctrl+V is not mistaken for an auto-repeat of the V that began the search.
        h.Down(GestureKey.Modifier);
        Assert.True(h.Tap(GestureKey.Trigger));
        h.Up(GestureKey.Modifier);
        await WaitForEffectAsync<PasteCurrentClipboard>();
        Assert.Equal(1, h.Paste.Count);
    }

    [Fact]
    public async Task Search_InputBeforeOrAfterASearch_IsIgnored()
    {
        h.Seed("apple");
        await h.StartAsync();

        Assert.False(h.Engine.OnSearchInput(new PickerSearchInput(PickerSearchKey.Enter)));
        await BeginSearchAsync();
        h.Picker.Search(new PickerSearchInput(PickerSearchKey.Escape));
        Assert.False(h.Picker.Search(new PickerSearchInput(PickerSearchKey.Enter)));
        await WaitForEffectAsync<GestureCancelled>();

        Assert.Equal(0, h.Paste.Count);
    }

    [Fact]
    public async Task Search_ThatCannotBegin_HidesThePicker_AndGivesTheKeysBackToTheGesture()
    {
        h.Seed("apple", "banana");
        h.Picker.ThrowOnBeginSearch = true;
        await h.StartAsync();

        await BeginSearchAsync();
        await WaitForAsync(() => h.Picker.Calls[^1].Method == "Hide", "the picker hidden");

        Assert.False(h.Engine.IsSearching);
        Assert.False(h.Picker.Search(new PickerSearchInput(PickerSearchKey.Enter)));
        h.Down(GestureKey.Modifier);
        Assert.True(h.Tap(GestureKey.Trigger));
        Assert.True(h.Engine.IsGestureActive);
        h.Up(GestureKey.Modifier);
    }

    [Fact]
    public async Task Search_FindsItemsByTheirFullText()
    {
        h.Seed("line one\n" + string.Concat(Enumerable.Repeat("filler ", 100)) + "\nhidden needle", "other");
        await h.StartAsync();
        await BeginSearchAsync();

        h.Picker.Search(PickerSearchInput.Typed("needle"));
        await WaitForAsync(() => h.Picker.Calls[^1].State?.SearchQuery == "needle", "the filtered picker");

        Assert.StartsWith("line one", Assert.Single(h.Picker.Calls[^1].State!.Items).Snapshot.Text, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- coaching tips

    private async Task CopyAsync(string text, string? from)
    {
        h.Focus(from);
        h.Backend.Current = EngineHarness.Text(text);
        await h.RaiseClipboardChangeAndWaitAsync();
    }

    private async Task QuickPasteAsync(string into)
    {
        h.Focus(into);
        var count = h.Processed.Count;
        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Up(GestureKey.Modifier);
        await h.WaitForEffectsAsync(count + 1);
    }

    [Fact]
    public async Task CopyingSomethingAlreadyInTheHistory_ShowsATip()
    {
        await h.StartAsync();
        await CopyAsync("a", "Editor");
        await CopyAsync("b", "Editor");

        await CopyAsync("a", "Editor");

        var tip = Assert.Single(h.Notifier.Tips);
        Assert.Equal(CoachingTipKind.CopiedAgain, tip.Kind);
        Assert.Equal(2, h.Notifier.Calls.Count);
    }

    [Fact]
    public async Task WhatIsOnTheClipboardAtStartup_IsNotCoached_WhenCopiedAgain()
    {
        h.Seed("a", "b");
        await h.StartAsync();

        await CopyAsync("b", "Editor");

        Assert.Empty(h.Notifier.Tips);
    }

    [Fact]
    public async Task CopyPasteElsewhereAndBack_ShowsTheTipInsteadOfTheCopiedNotification()
    {
        await h.StartAsync();
        await CopyAsync("a", "Photoshop");
        await QuickPasteAsync("Paint");
        h.Time.Advance(TimeSpan.FromSeconds(10));

        await CopyAsync("b", "Photoshop");

        var tip = Assert.Single(h.Notifier.Tips);
        Assert.Equal(CoachingTipKind.CopiedBackAndForth, tip.Kind);
        Assert.Contains("Photoshop", tip.Message);
        Assert.Contains("Paint", tip.Message);

        // Only the first copy was announced as "copied": the tip took the second's place.
        Assert.Equal(["a"], h.Notifier.Calls.Select(call => call.Item.Snapshot.Text));
    }

    [Fact]
    public async Task CopyPasteElsewhereAndBack_AfterThirtySeconds_ShowsNoTip()
    {
        await h.StartAsync();
        await CopyAsync("a", "Photoshop");
        await QuickPasteAsync("Paint");
        h.Time.Advance(TimeSpan.FromSeconds(31));

        await CopyAsync("b", "Photoshop");

        Assert.Empty(h.Notifier.Tips);
        Assert.Equal(2, h.Notifier.Calls.Count);
    }

    [Fact]
    public async Task PastingThroughThePicker_ShowsNoBackAndForthTip()
    {
        h.Seed("x");
        await h.StartAsync();
        await CopyAsync("a", "Photoshop");
        h.Focus("Paint");

        // Hold the gesture until the picker shows, then release: the clipboard is pasted, but the user saw the history.
        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        h.Up(GestureKey.Modifier);
        await h.WaitForEffectsAsync(4);

        await CopyAsync("b", "Photoshop");

        Assert.Empty(h.Notifier.Tips);
    }

    // ---------------------------------------------------------------- quick tap

    [Fact]
    public async Task QuickTap_PastesTheCurrentClipboard_WithoutWritingOrShowingThePicker()
    {
        var seeded = h.Seed("a", "b", "c");
        await h.StartAsync();

        Assert.False(h.Down(GestureKey.Modifier));
        Assert.True(h.Down(GestureKey.Trigger));
        Assert.True(h.Up(GestureKey.Trigger));
        Assert.False(h.Up(GestureKey.Modifier));

        var effects = await h.WaitForEffectsAsync(1);

        Assert.IsType<PasteCurrentClipboard>(Assert.Single(effects));
        Assert.Equal(1, h.Paste.Count);
        Assert.Equal("c", h.Paste.ClipboardAtPaste[0].Text);
        Assert.Empty(h.Backend.Written);
        Assert.Empty(h.Picker.Calls);
        Assert.Equal(Ids(seeded), Ids(h.History.Items));
        Assert.False(h.Engine.IsGestureActive);
    }

    [Fact]
    public async Task StandardShortcut_EachTapOfV_PastesRightAway_WhileTheModifierIsHeld()
    {
        // Issue #639: Ctrl held, then V, Down, V, as when pasting into one row after another.
        h.Hook.GestureModifierPastes = true;
        h.Seed("a", "b");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        Assert.True(h.Tap(GestureKey.Trigger));
        await h.WaitForEffectsAsync(1);
        Assert.Equal(1, h.Paste.Count);
        Assert.False(h.Engine.IsGestureActive);

        Assert.False(h.Tap(GestureKey.Down));
        Assert.True(h.Tap(GestureKey.Trigger));
        var effects = await h.WaitForEffectsAsync(2);

        Assert.All(effects, effect => Assert.IsType<PasteCurrentClipboard>(effect));
        Assert.Equal(["b", "b"], h.Paste.ClipboardAtPaste.Select(snapshot => snapshot.Text));
        Assert.Empty(h.Picker.Calls);
        Assert.Empty(h.Backend.Written);

        Assert.False(h.Up(GestureKey.Modifier));
        Assert.Equal(2, h.Processed.Count);
    }

    [Fact]
    public async Task AlternativeShortcut_ReleasingV_WaitsForTheModifier_BeforePasting()
    {
        h.Hook.GestureModifierPastes = false;
        h.Seed("a");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        Assert.True(h.Engine.IsGestureActive);
        Assert.Equal(0, h.Paste.Count);

        h.Up(GestureKey.Modifier);
        Assert.IsType<PasteCurrentClipboard>(Assert.Single(await h.WaitForEffectsAsync(1)));
    }

    [Fact]
    public async Task QuickTap_WithEmptyHistory_StillPastes()
    {
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Up(GestureKey.Modifier);

        var effects = await h.WaitForEffectsAsync(1);

        Assert.IsType<PasteCurrentClipboard>(Assert.Single(effects));
        Assert.Equal(1, h.Paste.Count);
        Assert.Empty(h.Backend.Written);
        Assert.Empty(h.Picker.Calls);
    }

    [Fact]
    public async Task EmptyHistory_ShowsAnEmptyPicker_ThenStillPastes()
    {
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        h.Up(GestureKey.Modifier);

        var effects = await h.WaitForEffectsAsync(3);

        Assert.Collection(
            effects,
            effect => Assert.IsType<ShowPicker>(effect),
            effect => Assert.IsType<HidePicker>(effect),
            effect => Assert.IsType<PasteCurrentClipboard>(effect));
        var shown = h.Picker.Calls[0].State!;
        Assert.Empty(shown.Items);
        Assert.Empty(shown.Actions);
        Assert.Equal(1, h.Paste.Count);
        Assert.Empty(h.Backend.Written);
    }

    [Fact]
    public async Task QuickTap_PassesTheShutdownTokenToThePasteSimulator()
    {
        h.Seed("a");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Up(GestureKey.Modifier);
        await h.WaitForEffectsAsync(1);

        Assert.True(h.Paste.LastToken.CanBeCanceled);
        Assert.False(h.Paste.LastToken.IsCancellationRequested);
    }

    // ---------------------------------------------------------------- selecting an older item

    [Fact]
    public async Task SecondV_ShowsThePicker_AndReleasePastesTheSecondItem()
    {
        var seeded = h.Seed("a", "b", "c");
        Assert.Equal(["c", "b", "a"], Texts(seeded));
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        h.Up(GestureKey.Modifier);

        var effects = await h.WaitForEffectsAsync(3);

        Assert.Collection(
            effects,
            effect => Assert.IsType<ShowPicker>(effect),
            effect => Assert.IsType<HidePicker>(effect),
            effect => Assert.Equal(new PerformAction(seeded[1].Id, 0), effect));

        // The presenter was shown with the session's items and the second item selected, then hidden.
        var calls = h.Picker.Calls;
        Assert.Equal(["Show", "Hide"], calls.Select(call => call.Method));
        var shown = calls[0].State!;
        Assert.Equal(Ids(seeded), Ids(shown.Items));
        Assert.Equal(1, shown.SelectedIndex);
        Assert.Equal(DefaultTextActions, shown.Actions.Select(action => action.Title));
        Assert.Equal(0, shown.SelectedActionIndex);
        Assert.Equal(PickerPane.Items, shown.Pane);

        // The second item's snapshot went to the clipboard before the paste keystroke.
        var written = Assert.Single(h.Backend.Written);
        Assert.Same(seeded[1].Snapshot, written);
        Assert.Equal(1, h.Paste.Count);
        Assert.Same(seeded[1].Snapshot, h.Paste.ClipboardAtPaste[0]);
        Assert.Equal(["picker.Show", "picker.Hide", "clipboard.write:b", "paste"], h.Log.Entries);

        // The pasted item became the newest.
        Assert.Equal(["b", "c", "a"], Texts(h.History.Items));
        Assert.False(h.Engine.IsGestureActive);
    }

    [Fact]
    public async Task ThirdV_SelectsTheThirdItem_AndUpdatesThePicker()
    {
        var seeded = h.Seed("a", "b", "c");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        h.Up(GestureKey.Modifier);

        var effects = await h.WaitForEffectsAsync(4);

        Assert.Equal(new PerformAction(seeded[2].Id, 0), effects[3]);
        var calls = h.Picker.Calls;
        Assert.Equal(["Show", "Update", "Hide"], calls.Select(call => call.Method));
        Assert.Equal(1, calls[0].State!.SelectedIndex);
        Assert.Equal(2, calls[1].State!.SelectedIndex);
        Assert.Same(seeded[2].Snapshot, Assert.Single(h.Backend.Written));
        Assert.Equal(["a", "c", "b"], Texts(h.History.Items));
    }

    [Fact]
    public async Task PickerActions_AreTheTitlesOfTheSelectedItemsActions()
    {
        h.Seed("see https://example.com/page", "plain");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Escape);
        h.Up(GestureKey.Modifier);
        await h.WaitForEffectsAsync(3);

        var shown = h.Picker.Calls[0].State!;
        Assert.Equal(1, shown.SelectedIndex);
        Assert.Equal("see https://example.com/page", shown.Items[1].Snapshot.Text);
        Assert.Equal(["Paste", "Paste as file", "Open link", "Pin to clipboard"], shown.Actions.Select(action => action.Title));
    }

    [Fact]
    public async Task NavigatingBackToTheDefaultItem_StillPerformsTheAction()
    {
        var seeded = h.Seed("a", "b", "c");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        Assert.True(h.Tap(GestureKey.Up));
        h.Up(GestureKey.Modifier);

        var effects = await h.WaitForEffectsAsync(4);

        Assert.IsType<UpdatePicker>(effects[1]);
        Assert.Equal(0, h.Picker.Calls[1].State!.SelectedIndex);
        Assert.Equal(new PerformAction(seeded[0].Id, 0), effects[3]);
        Assert.Same(seeded[0].Snapshot, Assert.Single(h.Backend.Written));
        Assert.Equal(1, h.Paste.Count);
    }

    [Fact]
    public async Task Session_IsSnapshottedWhenTheGestureBegins()
    {
        var seeded = h.Seed("a", "b", "c");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);

        // A copy that lands while the gesture is in progress does not shift the picker's items.
        h.History.Add(EngineHarness.Text("late"));
        h.Tap(GestureKey.Trigger);
        h.Up(GestureKey.Modifier);

        var effects = await h.WaitForEffectsAsync(3);

        Assert.Equal(Ids(seeded), Ids(h.Picker.Calls[0].State!.Items));
        Assert.Equal(new PerformAction(seeded[1].Id, 0), effects[2]);
        Assert.Same(seeded[1].Snapshot, Assert.Single(h.Backend.Written));
    }

    [Fact]
    public async Task ItemRemovedFromHistoryDuringTheGesture_IsStillPastedFromTheSession()
    {
        var seeded = h.Seed("a", "b", "c");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        h.History.Remove(seeded[1].Id);
        h.Up(GestureKey.Modifier);

        await h.WaitForEffectsAsync(3);

        Assert.Same(seeded[1].Snapshot, Assert.Single(h.Backend.Written));
        Assert.Equal(1, h.Paste.Count);
        Assert.Equal(["c", "a"], Texts(h.History.Items));
    }

    [Fact]
    public async Task PinnedItems_AreListedFirst_AndTheNewestUnpinnedItemIsTheDefault()
    {
        var pinned = await h.AddPinnedAsync("pinned");
        var seeded = h.Seed("a", "b");
        Assert.Equal(["pinned", "b", "a"], Texts(seeded));
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        h.Up(GestureKey.Modifier);

        var effects = await h.WaitForEffectsAsync(3);

        var shown = h.Picker.Calls[0].State!;
        Assert.Equal(Ids(seeded), Ids(shown.Items));
        Assert.Equal(2, shown.SelectedIndex);
        Assert.Equal(["Paste", "Paste as file", "Pin to clipboard"], shown.Actions.Select(action => action.Title));
        Assert.Equal(new PerformAction(seeded[2].Id, 0), effects[2]);
        Assert.Equal(["pinned", "a", "b"], Texts(h.History.Items));
        Assert.True(h.History.Find(pinned.Id)!.IsPinned);
    }

    [Fact]
    public async Task SelectingAPinnedItem_ShowsUnpinAndPastesWithoutMovingIt()
    {
        var pinned = await h.AddPinnedAsync("pinned");
        h.Seed("a");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger); // wraps from "a" (index 1) to the pinned item (index 0)
        h.Up(GestureKey.Modifier);

        var effects = await h.WaitForEffectsAsync(3);

        var shown = h.Picker.Calls[0].State!;
        Assert.Equal(0, shown.SelectedIndex);
        Assert.Equal(["Paste", "Paste as file", "Unpin from clipboard"], shown.Actions.Select(action => action.Title));
        Assert.Equal(new PerformAction(pinned.Id, 0), effects[2]);
        Assert.Same(pinned.Snapshot, Assert.Single(h.Backend.Written));
        Assert.Equal(["pinned", "a"], Texts(h.History.Items));
    }

    // ---------------------------------------------------------------- the actions pane

    [Fact]
    public async Task ActionsPane_PerformsTheSelectedAction_OnTheSelectedItem()
    {
        RecordingAction? record = null;
        await UseCatalogAsync(log => new ActionCatalog([new PasteAction(), record = new RecordingAction("record", 5, "Record", log)]));
        var seeded = h.Seed("a", "b", "c");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        Assert.True(h.Tap(GestureKey.Right));
        Assert.True(h.Tap(GestureKey.Down));
        h.Up(GestureKey.Modifier);

        var effects = await h.WaitForEffectsAsync(5);

        Assert.Equal(new PerformAction(seeded[1].Id, 1), effects[4]);
        var calls = h.Picker.Calls;
        Assert.Equal(["Show", "Update", "Update", "Hide"], calls.Select(call => call.Method));
        Assert.Equal(["Paste", "Record"], calls[0].State!.Actions.Select(action => action.Title));
        Assert.Equal(PickerPane.Actions, calls[1].State!.Pane);
        Assert.Equal(0, calls[1].State!.SelectedActionIndex);
        Assert.Equal(PickerPane.Actions, calls[2].State!.Pane);
        Assert.Equal(1, calls[2].State!.SelectedActionIndex);
        Assert.Equal(1, calls[2].State!.SelectedIndex);

        Assert.Same(seeded[1], Assert.Single(record!.Executed));
        Assert.Empty(h.Backend.Written);
        Assert.Equal(0, h.Paste.Count);
        Assert.Equal(Ids(seeded), Ids(h.History.Items));
    }

    [Fact]
    public async Task ActionsOrder_FollowsTheCatalogOrder()
    {
        await UseCatalogAsync(log => new ActionCatalog(
        [
            new RecordingAction("late", 50, "Late", log),
            new RecordingAction("early", 1, "Early", log),
            new PasteAction(),
        ]));
        h.Seed("a", "b");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Escape);
        h.Up(GestureKey.Modifier);
        await h.WaitForEffectsAsync(3);

        Assert.Equal(["Paste", "Early", "Late"], h.Picker.Calls[0].State!.Actions.Select(action => action.Title));
    }

    [Fact]
    public async Task FailingAction_IsLogged_AndLaterGesturesStillWork()
    {
        RecordingAction? failing = null;
        await UseCatalogAsync(log => new ActionCatalog([failing = new RecordingAction("fail", 0, "Fail", log) { Throw = true }]));
        h.Seed("a", "b");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        h.Up(GestureKey.Modifier);
        var effects = await h.WaitForEffectsAsync(3);

        Assert.IsType<PerformAction>(effects[2]);
        Assert.Single(failing!.Executed);
        h.Logger.Received(1).Error(Arg.Any<InvalidOperationException>(), Arg.Any<string>(), nameof(PerformAction));

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Up(GestureKey.Modifier);
        effects = await h.WaitForEffectsAsync(4);

        Assert.IsType<PasteCurrentClipboard>(effects[3]);
        Assert.Equal(1, h.Paste.Count);
    }

    [Fact]
    public async Task FailingPresenter_DoesNotStopTheEffectLoop()
    {
        h.Picker.ThrowOnShow = true;
        var seeded = h.Seed("a", "b");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        h.Up(GestureKey.Modifier);

        var effects = await h.WaitForEffectsAsync(3);

        Assert.Equal(new PerformAction(seeded[1].Id, 0), effects[2]);
        Assert.Equal(1, h.Paste.Count);
        h.Logger.Received(1).Error(Arg.Any<InvalidOperationException>(), Arg.Any<string>(), nameof(ShowPicker));
    }

    // ---------------------------------------------------------------- the show delay

    [Fact]
    public async Task HoldingPastThePasteDelay_ShowsThePickerViaTheTimer()
    {
        var seeded = h.Seed("a", "b", "c");
        h.Engine.PasteDelay = TimeSpan.FromMilliseconds(300);
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Time.Advance(TimeSpan.FromMilliseconds(250));
        Assert.Empty(h.Processed);

        h.Time.Advance(TimeSpan.FromMilliseconds(75));
        var effects = await h.WaitForEffectsAsync(1);

        var show = Assert.IsType<ShowPicker>(Assert.Single(effects));
        Assert.Equal(0, show.Selection.SelectedIndex);
        var shown = Assert.Single(h.Picker.Calls).State!;
        Assert.Equal("Show", h.Picker.Calls[0].Method);
        Assert.Equal(Ids(seeded), Ids(shown.Items));
        Assert.Equal(0, shown.SelectedIndex);
        Assert.Equal(DefaultTextActions, shown.Actions.Select(action => action.Title));

        // More ticks do not show it again.
        h.Time.Advance(TimeSpan.FromMilliseconds(500));

        // Releasing without changing the selection hides the picker and pastes the clipboard untouched.
        h.Up(GestureKey.Modifier);
        effects = await h.WaitForEffectsAsync(3);

        Assert.Collection(
            effects,
            effect => Assert.IsType<ShowPicker>(effect),
            effect => Assert.IsType<HidePicker>(effect),
            effect => Assert.IsType<PasteCurrentClipboard>(effect));
        Assert.Equal(1, h.Paste.Count);
        Assert.Empty(h.Backend.Written);
        Assert.Equal(Ids(seeded), Ids(h.History.Items));
    }

    [Fact]
    public async Task ReleasingBeforeThePasteDelay_NeverShowsThePicker()
    {
        h.Seed("a", "b");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Time.Advance(TimeSpan.FromMilliseconds(200));
        h.Up(GestureKey.Modifier);
        h.Time.Advance(TimeSpan.FromSeconds(2));

        var effects = await h.WaitForEffectsAsync(1);

        Assert.IsType<PasteCurrentClipboard>(Assert.Single(effects));
        Assert.Empty(h.Picker.Calls);
    }

    [Fact]
    public async Task HoldingWithEmptyHistory_ShowsAnEmptyPicker()
    {
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Time.Advance(TimeSpan.FromSeconds(2));
        await h.WaitForEffectsAsync(1);
        h.Up(GestureKey.Modifier);

        var effects = await h.WaitForEffectsAsync(3);

        Assert.Collection(
            effects,
            effect => Assert.IsType<ShowPicker>(effect),
            effect => Assert.IsType<HidePicker>(effect),
            effect => Assert.IsType<PasteCurrentClipboard>(effect));
        Assert.Equal(["Show", "Hide"], h.Picker.Calls.Select(call => call.Method));
        Assert.Empty(h.Picker.Calls[0].State!.Items);
    }

    [Fact]
    public async Task ZeroPasteDelay_ShowsThePickerOnTheFirstV()
    {
        h.Seed("a", "b");
        h.Engine.PasteDelay = TimeSpan.Zero;
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        Assert.True(h.Tap(GestureKey.Trigger));

        var effects = await h.WaitForEffectsAsync(1);

        Assert.IsType<ShowPicker>(Assert.Single(effects));
        Assert.Equal(0, h.Picker.Calls[0].State!.SelectedIndex);
    }

    [Fact]
    public void PasteDelay_RoundTrips_AndDefaultsTo300Milliseconds()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(300), h.Engine.PasteDelay);

        h.Engine.PasteDelay = TimeSpan.FromMilliseconds(123);

        Assert.Equal(TimeSpan.FromMilliseconds(123), h.Engine.PasteDelay);
    }

    [Fact]
    public async Task IsGestureActive_TracksTheGesture()
    {
        h.Seed("a");
        await h.StartAsync();
        Assert.False(h.Engine.IsGestureActive);

        h.Down(GestureKey.Modifier);
        Assert.False(h.Engine.IsGestureActive);

        h.Down(GestureKey.Trigger);
        Assert.True(h.Engine.IsGestureActive);

        h.Up(GestureKey.Trigger);
        Assert.True(h.Engine.IsGestureActive);

        h.Up(GestureKey.Modifier);
        Assert.False(h.Engine.IsGestureActive);
        await h.WaitForEffectsAsync(1);
    }

    // ---------------------------------------------------------------- delete

    [Fact]
    public async Task Delete_RemovesTheSelectedItemFromHistory_AndUpdatesThePicker()
    {
        var pinned = await h.AddPinnedAsync("pinned");
        var seeded = h.Seed("a", "b", "c");
        Assert.Equal(["pinned", "c", "b", "a"], Texts(seeded));
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger); // selects "b"
        Assert.True(h.Down(GestureKey.Delete));
        Assert.True(h.Up(GestureKey.Delete));

        var effects = await h.WaitForEffectsAsync(3);

        Assert.Equal(new DeleteItem(seeded[2].Id), effects[1]);
        Assert.IsType<UpdatePicker>(effects[2]);
        Assert.Null(h.History.Find(seeded[2].Id));
        Assert.Equal(["pinned", "c", "a"], Texts(h.History.Items));

        var updated = h.Picker.Calls[1].State!;
        Assert.Equal("Update", h.Picker.Calls[1].Method);
        Assert.Equal([seeded[0].Id, seeded[1].Id, seeded[3].Id], Ids(updated.Items));
        Assert.Equal(2, updated.SelectedIndex);
        Assert.Equal(PickerPane.Items, updated.Pane);

        // Deleting an unpinned item leaves the pinned store alone.
        var stored = await h.PinnedStore.LoadAllAsync(Ct);
        Assert.Equal([pinned.Id], Ids(stored));

        h.Tap(GestureKey.Escape);
        h.Up(GestureKey.Modifier);
        await h.WaitForEffectsAsync(5);
        Assert.Equal(0, h.Paste.Count);
        Assert.Empty(h.Backend.Written);
    }

    [Fact]
    public async Task Delete_ThenRelease_PastesTheNewSelection()
    {
        var seeded = h.Seed("a", "b", "c");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger); // selects "b"
        h.Tap(GestureKey.Delete); // selection moves to "a"
        h.Up(GestureKey.Modifier);

        var effects = await h.WaitForEffectsAsync(5);

        Assert.Equal(new PerformAction(seeded[2].Id, 0), effects[4]);
        Assert.Same(seeded[2].Snapshot, Assert.Single(h.Backend.Written));
        Assert.Equal(["a", "c"], Texts(h.History.Items));
    }

    [Fact]
    public async Task Delete_PinnedItem_RemovesItFromHistoryAndThePinnedStore()
    {
        var keep = await h.AddPinnedAsync("keep");
        var doomed = await h.AddPinnedAsync("doomed");
        h.Seed("a");
        Assert.Equal(["keep", "doomed", "a"], Texts(h.History.Items));
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger); // wraps from "a" (index 2) to "keep" (index 0)
        h.Tap(GestureKey.Down); // "doomed"
        h.Tap(GestureKey.Delete);

        var effects = await h.WaitForEffectsAsync(4);

        Assert.Equal(new DeleteItem(doomed.Id), effects[2]);
        Assert.Null(h.History.Find(doomed.Id));
        Assert.Equal(["keep", "a"], Texts(h.History.Items));
        var stored = await h.PinnedStore.LoadAllAsync(Ct);
        Assert.Equal([keep.Id], Ids(stored));
        Assert.False(Directory.Exists(Path.Combine(h.Paths.PinnedDirectory, doomed.Id.ToString("N"))));

        h.Tap(GestureKey.Escape);
        h.Up(GestureKey.Modifier);
        await h.WaitForEffectsAsync(6);
        Assert.Equal(0, h.Paste.Count);
    }

    [Fact]
    public async Task Delete_TheOnlyItem_HidesThePickerAndCancels()
    {
        var seeded = h.Seed("only");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Delete);

        var effects = await h.WaitForEffectsAsync(4);

        Assert.Collection(
            effects,
            effect => Assert.IsType<ShowPicker>(effect),
            effect => Assert.Equal(new DeleteItem(seeded[0].Id), effect),
            effect => Assert.IsType<HidePicker>(effect),
            effect => Assert.IsType<GestureCancelled>(effect));
        Assert.Empty(h.History.Items);
        Assert.False(h.Engine.IsGestureActive);
        Assert.Equal([true, false], h.Hook.NavigationCaptureCalls);

        Assert.False(h.Up(GestureKey.Modifier));
        Assert.Equal(0, h.Paste.Count);
        await AssertNoFurtherEffectsAsync(4);
    }

    // ---------------------------------------------------------------- escape

    [Fact]
    public async Task Escape_HidesThePickerAndPastesNothing()
    {
        h.Seed("a", "b", "c");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        Assert.True(h.Down(GestureKey.Escape));
        Assert.False(h.Engine.IsGestureActive);
        Assert.False(h.Up(GestureKey.Escape));
        Assert.False(h.Up(GestureKey.Modifier));

        var effects = await h.WaitForEffectsAsync(3);

        Assert.Collection(
            effects,
            effect => Assert.IsType<ShowPicker>(effect),
            effect => Assert.IsType<HidePicker>(effect),
            effect => Assert.IsType<GestureCancelled>(effect));
        Assert.Equal(["Show", "Hide"], h.Picker.Calls.Select(call => call.Method));
        Assert.Equal(0, h.Paste.Count);
        Assert.Empty(h.Backend.Written);
        await AssertNoFurtherEffectsAsync(3);
    }

    [Fact]
    public async Task Escape_BeforeThePickerIsShown_OnlyCancels()
    {
        h.Seed("a", "b");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        Assert.True(h.Tap(GestureKey.Escape));
        h.Up(GestureKey.Modifier);

        var effects = await h.WaitForEffectsAsync(1);

        Assert.IsType<GestureCancelled>(Assert.Single(effects));
        Assert.Empty(h.Picker.Calls);
        Assert.Equal(0, h.Paste.Count);
        await AssertNoFurtherEffectsAsync(1);
    }

    // ---------------------------------------------------------------- suppression

    [Fact]
    public async Task OnKey_ReturnsWhetherTheKeyIsSuppressed()
    {
        h.Seed("a", "b");
        await h.StartAsync();

        // Outside a gesture nothing is suppressed.
        Assert.False(h.Engine.OnKey(new KeyInput(GestureKey.Trigger, true)));
        Assert.False(h.Engine.OnKey(new KeyInput(GestureKey.Trigger, false)));
        Assert.False(h.Engine.OnKey(new KeyInput(GestureKey.Down, true)));
        Assert.False(h.Engine.OnKey(new KeyInput(GestureKey.Escape, true)));
        Assert.False(h.Engine.OnKey(new KeyInput(GestureKey.Other, true)));

        // The modifier always reaches the application; V and navigation keys are swallowed during a gesture.
        Assert.False(h.Engine.OnKey(new KeyInput(GestureKey.Modifier, true)));
        Assert.True(h.Engine.OnKey(new KeyInput(GestureKey.Trigger, true)));
        Assert.True(h.Engine.OnKey(new KeyInput(GestureKey.Trigger, true))); // auto-repeat
        Assert.True(h.Engine.OnKey(new KeyInput(GestureKey.Trigger, false)));
        Assert.True(h.Engine.OnKey(new KeyInput(GestureKey.Down, true)));
        Assert.True(h.Engine.OnKey(new KeyInput(GestureKey.Down, false)));
        Assert.True(h.Engine.OnKey(new KeyInput(GestureKey.Right, true)));
        Assert.True(h.Engine.OnKey(new KeyInput(GestureKey.Right, false)));
        Assert.False(h.Engine.OnKey(new KeyInput(GestureKey.Other, true)));
        Assert.False(h.Engine.OnKey(new KeyInput(GestureKey.Modifier, false)));

        // Down showed the picker; Right updated it; release hid it and pasted.
        await h.WaitForEffectsAsync(4);

        // Once the gesture is over, keys pass through again.
        Assert.False(h.Engine.OnKey(new KeyInput(GestureKey.Down, true)));
        Assert.False(h.Engine.OnKey(new KeyInput(GestureKey.Down, false)));
    }

    [Fact]
    public async Task KeyHandlerInstalledOnTheHook_RoutesToOnKey()
    {
        h.Seed("a");
        await h.StartAsync();

        Assert.False(h.Hook.KeyHandler!(new KeyInput(GestureKey.Modifier, true)));
        Assert.True(h.Hook.KeyHandler!(new KeyInput(GestureKey.Trigger, true)));
        Assert.True(h.Engine.IsGestureActive);
        Assert.True(h.Hook.KeyHandler!(new KeyInput(GestureKey.Trigger, false)));
        Assert.False(h.Hook.KeyHandler!(new KeyInput(GestureKey.Modifier, false)));

        await h.WaitForEffectsAsync(1);
        Assert.Equal(1, h.Paste.Count);
    }

    [Fact]
    public async Task AutoRepeatOfVAfterTheGestureEnded_IsSwallowedAndStartsNothing()
    {
        h.Seed("a");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        h.Down(GestureKey.Trigger);
        h.Up(GestureKey.Modifier);
        await h.WaitForEffectsAsync(1);

        // V is still physically held and auto-repeats.
        Assert.True(h.Down(GestureKey.Trigger));
        Assert.False(h.Engine.IsGestureActive);
        Assert.True(h.Up(GestureKey.Trigger));
        Assert.Equal([true, false], h.Hook.NavigationCaptureCalls);
        await AssertNoFurtherEffectsAsync(1);
    }

    // ---------------------------------------------------------------- navigation capture

    [Fact]
    public async Task NavigationCapture_IsEnabledWhenAGestureStarts_AndDisabledWhenItEnds()
    {
        h.Seed("a", "b");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        Assert.Empty(h.Hook.NavigationCaptureCalls);

        h.Down(GestureKey.Trigger);
        Assert.Equal([true], h.Hook.NavigationCaptureCalls);

        // Further keys during the gesture do not toggle it again.
        h.Up(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Down);
        h.Tap(GestureKey.Other);
        Assert.Equal([true], h.Hook.NavigationCaptureCalls);

        h.Up(GestureKey.Modifier);
        Assert.Equal([true, false], h.Hook.NavigationCaptureCalls);

        // The next gesture toggles it again; Escape ends it immediately.
        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Down(GestureKey.Escape);
        Assert.Equal([true, false, true, false], h.Hook.NavigationCaptureCalls);
        h.Up(GestureKey.Escape);
        h.Up(GestureKey.Modifier);
        Assert.Equal([true, false, true, false], h.Hook.NavigationCaptureCalls);
    }

    [Fact]
    public async Task NavigationCapture_IsNotTouchedWhenNoGestureStarts()
    {
        await h.StartAsync();

        h.Tap(GestureKey.Trigger);
        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Down);
        h.Tap(GestureKey.Escape);
        h.Up(GestureKey.Modifier);

        Assert.Empty(h.Hook.NavigationCaptureCalls);
    }

    [Fact]
    public async Task NavigationCaptureFailure_IsLogged_AndTheGestureStillWorks()
    {
        h.Hook.ThrowOnNavigationCapture = true;
        var seeded = h.Seed("a", "b");
        await h.StartAsync();

        h.Down(GestureKey.Modifier);
        Assert.True(h.Down(GestureKey.Trigger));
        h.Up(GestureKey.Trigger);
        Assert.True(h.Tap(GestureKey.Trigger));
        Assert.False(h.Up(GestureKey.Modifier));

        var effects = await h.WaitForEffectsAsync(3);

        Assert.Equal(new PerformAction(seeded[1].Id, 0), effects[2]);
        Assert.Equal([true, false], h.Hook.NavigationCaptureCalls);
        h.Logger.Received(2).Warning(Arg.Any<InvalidOperationException>(), Arg.Any<string>());
    }

    // ---------------------------------------------------------------- disposal

    [Fact]
    public async Task DisposeAsync_RemovesTheKeyHandler_AndStopsCopyNotifications()
    {
        await h.StartAsync();

        await h.DisposeEngineAsync();

        Assert.Null(h.Hook.KeyHandler);

        // The monitor is owned by the caller and keeps capturing, but the engine no longer notifies.
        h.Backend.Current = EngineHarness.Text("after");
        await h.RaiseClipboardChangeAndWaitAsync();
        Assert.Equal(["after"], Texts(h.History.Items));
        Assert.Empty(h.Notifier.Calls);
    }

    [Fact]
    public async Task DisposeAsync_StopsProcessingEffects()
    {
        h.Seed("a", "b");
        await h.StartAsync();
        await h.DisposeEngineAsync();

        // Keys that still arrive (for example from a hook thread racing with shutdown) are harmless.
        h.Engine.OnKey(new KeyInput(GestureKey.Modifier, true));
        h.Engine.OnKey(new KeyInput(GestureKey.Trigger, true));
        h.Time.Advance(TimeSpan.FromSeconds(1));
        h.Engine.OnKey(new KeyInput(GestureKey.Modifier, false));

        await Task.Delay(50, Ct);
        Assert.Empty(h.Processed);
        Assert.Empty(h.Picker.Calls);
        Assert.Equal(0, h.Paste.Count);
    }

    [Fact]
    public async Task DisposeAsync_DuringAGesture_CompletesPromptly()
    {
        h.Seed("a", "b");
        await h.StartAsync();
        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Tap(GestureKey.Trigger);
        await h.WaitForEffectsAsync(1);

        var dispose = h.DisposeEngineAsync().AsTask();

        await dispose.WaitAsync(TimeSpan.FromSeconds(2), Ct);
        Assert.Null(h.Hook.KeyHandler);
    }

    [Fact]
    public async Task DisposeAsync_WithoutStart_DoesNotThrow()
    {
        await h.DisposeEngineAsync();

        Assert.Null(h.Hook.KeyHandler);
        Assert.Equal(0, h.Hook.StartCount);
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        await h.StartAsync();

        await h.DisposeEngineAsync();
        var exception = await Record.ExceptionAsync(async () => await h.Engine.DisposeAsync());

        Assert.Null(exception);
    }

    /// <summary>Proves that no effect beyond <paramref name="count"/> is pending by sending a sentinel gesture through the FIFO.</summary>
    private async Task AssertNoFurtherEffectsAsync(int count)
    {
        var pastes = h.Paste.Count;
        h.Down(GestureKey.Modifier);
        h.Tap(GestureKey.Trigger);
        h.Up(GestureKey.Modifier);

        var effects = await h.WaitForEffectsAsync(count + 1);

        Assert.Equal(count + 1, effects.Count);
        Assert.IsType<PasteCurrentClipboard>(effects[count]);
        Assert.Equal(pastes + 1, h.Paste.Count);
    }
}

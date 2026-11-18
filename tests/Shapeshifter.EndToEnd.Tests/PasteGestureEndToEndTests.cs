using SharpHook.Data;

namespace Shapeshifter.EndToEnd.Tests;

/// <summary>
/// Copies through the real OS clipboard and pastes into a separate process with real keystrokes.
/// Every assertion checks the exact text that arrived in the target, so a paste that leaked through
/// unsuppressed (the target pasting by itself as well) shows up as duplicated text.
/// </summary>
[Collection(nameof(EndToEndCollection))]
public sealed class PasteGestureEndToEndTests(ShapeshifterSession session)
{
    [Fact]
    public async Task QuickPastePastesTheCurrentClipboard()
    {
        SkipUnlessEnabled();
        await session.CopyTextAsync("alpha " + Unique());
        var expected = session.ReadState().Previews[0];
        await session.PrepareTargetAsync();

        await session.PressModifierAsync();
        await session.TapAsync(KeyCode.VcV);
        await session.ReleaseModifierAsync();

        Assert.Equal(expected, await session.WaitForTargetTextAsync(expected));
        await session.WaitForIdleAsync();
    }

    [Fact]
    public async Task HoldingTheModifier_EachQuickVPastesRightAway()
    {
        // Issue #639: Ctrl stays down while V is pressed again and again, as when pasting into one row after another.
        SkipUnlessEnabled();
        await session.CopyTextAsync("again " + Unique());
        var expected = session.ReadState().Previews[0];
        await session.PrepareTargetAsync();

        await session.PressModifierAsync();
        await session.TapAsync(KeyCode.VcV);
        Assert.Equal(expected, await session.WaitForTargetTextAsync(expected));
        await session.WaitForIdleAsync();
        await session.TapAsync(KeyCode.VcV);
        Assert.Equal(expected + expected, await session.WaitForTargetTextAsync(expected + expected));
        await session.WaitForIdleAsync();
        Assert.False(session.ReadState().PickerVisible);
        await session.ReleaseModifierAsync();
        await session.WaitForIdleAsync();

        // Nothing more is pasted when the modifier comes up.
        await Task.Delay(400, TestContext.Current.CancellationToken);
        Assert.Equal(expected + expected, await session.GetTargetTextAsync());
    }

    [Fact]
    public async Task PressingVAgainPastesTheOlderItem()
    {
        SkipUnlessEnabled();
        var id = Unique();
        await session.CopyTextAsync($"one {id}");
        await session.CopyTextAsync($"two {id}");
        await session.CopyTextAsync($"three {id}");
        await session.PrepareTargetAsync();

        await session.PressModifierAsync();
        await session.HoldVUntilThePickerShowsAsync();
        await session.TapAsync(KeyCode.VcV);
        await session.WaitForStateAsync(state => state.PickerVisible && state.SelectedIndex == 1, "picker on the second item");
        await session.ReleaseModifierAsync();

        Assert.Equal($"two {id}", await session.WaitForTargetTextAsync($"two {id}"));
        var state = await session.WaitForStateAsync(s => !s.PickerVisible && s.Previews[0] == $"two {id}", "picker hidden and pasted item moved to the top");
        Assert.Equal($"three {id}", state.Previews[1]);
        await session.WaitForIdleAsync();
    }

    [Fact]
    public async Task HoldingShowsThePickerAndArrowKeysNavigate()
    {
        SkipUnlessEnabled();
        var id = Unique();
        await session.CopyTextAsync($"red {id}");
        await session.CopyTextAsync($"green {id}");
        await session.CopyTextAsync($"blue {id}");
        await session.PrepareTargetAsync();

        await session.PressModifierAsync();
        await session.HoldVUntilThePickerShowsAsync();
        await session.WaitForStateAsync(state => state.PickerVisible && state.SelectedIndex == 0, "picker on the newest item");
        await session.TapAsync(KeyCode.VcDown);
        await session.TapAsync(KeyCode.VcDown);
        await session.TapAsync(KeyCode.VcUp);
        await session.WaitForStateAsync(state => state.SelectedIndex == 1, "selection moved to the second item");
        await session.ReleaseModifierAsync();

        Assert.Equal($"green {id}", await session.WaitForTargetTextAsync($"green {id}"));
        await session.WaitForIdleAsync();
    }

    [Fact]
    public async Task DigitJumpsToThatItem_WithoutTypingTheDigit()
    {
        SkipUnlessEnabled();
        var id = Unique();
        await session.CopyTextAsync($"first {id}");
        await session.CopyTextAsync($"second {id}");
        await session.CopyTextAsync($"third {id}");
        await session.PrepareTargetAsync();

        await session.PressModifierAsync();
        await session.KeyAsync(KeyCode.VcV, down: true);
        await session.TapAsync(KeyCode.Vc3);
        await session.KeyAsync(KeyCode.VcV, down: false);
        await session.WaitForStateAsync(state => state.PickerVisible && state.SelectedIndex == 2, "picker on the third item");
        await session.ReleaseModifierAsync();

        // A digit that leaked to the target would show up in its text.
        Assert.Equal($"first {id}", await session.WaitForTargetTextAsync($"first {id}"));
        await session.WaitForIdleAsync();
    }

    [Fact]
    public async Task EscapeCancelsWithoutPasting()
    {
        SkipUnlessEnabled();
        var id = Unique();
        await session.CopyTextAsync($"keep {id}");
        await session.CopyTextAsync($"latest {id}");
        await session.PrepareTargetAsync();

        await session.PressModifierAsync();
        await session.HoldVUntilThePickerShowsAsync();
        await session.TapAsync(KeyCode.VcV);
        await session.WaitForStateAsync(state => state.PickerVisible, "picker visible");
        await session.TapAsync(KeyCode.VcEscape);
        await session.WaitForStateAsync(state => !state.PickerVisible, "picker hidden after Escape");
        await session.ReleaseModifierAsync();
        await session.WaitForIdleAsync();
        await Task.Delay(500, TestContext.Current.CancellationToken);

        Assert.Equal(string.Empty, await session.GetTargetTextAsync());
    }

    [Fact]
    public async Task SpaceKeepsThePickerOpen_AndEnterPastes_WithoutTypingTheKeys()
    {
        SkipUnlessEnabled();
        var id = Unique();
        await session.CopyTextAsync($"oak {id}");
        await session.CopyTextAsync($"elm {id}");
        await session.CopyTextAsync($"ash {id}");
        await session.PrepareTargetAsync();

        await session.PressModifierAsync();
        await session.KeyAsync(KeyCode.VcV, down: true);
        await session.TapAsync(KeyCode.VcSpace);
        await session.KeyAsync(KeyCode.VcV, down: false);
        await session.WaitForStateAsync(state => state.PickerVisible, "picker kept open after Space");
        await session.ReleaseModifierAsync();

        // Released, but still open: the arrows and digits work without the modifier and never reach the target.
        await Task.Delay(400, TestContext.Current.CancellationToken);
        Assert.True(session.ReadState().PickerVisible);
        await session.TapAsync(KeyCode.VcDown);
        await session.TapAsync(KeyCode.VcDown);
        await session.WaitForStateAsync(state => state.SelectedIndex == 2, "selection moved to the third item");
        await session.TapAsync(KeyCode.Vc2);
        await session.WaitForStateAsync(state => state.SelectedIndex == 1, "selection jumped to the second item");
        await session.TapAsync(KeyCode.VcEnter);

        // A Space, digit or Enter that leaked to the target would show up in its text.
        Assert.Equal($"elm {id}", await session.WaitForTargetTextAsync($"elm {id}"));
        await session.WaitForStateAsync(state => !state.PickerVisible, "picker hidden after Enter");
        await session.WaitForIdleAsync();
    }

    [Fact]
    public async Task EscapeClosesAPickerKeptOpen_WithoutPasting()
    {
        SkipUnlessEnabled();
        var id = Unique();
        await session.CopyTextAsync($"stay {id}");
        await session.CopyTextAsync($"newest {id}");
        await session.PrepareTargetAsync();

        await session.PressModifierAsync();
        await session.KeyAsync(KeyCode.VcV, down: true);
        await session.TapAsync(KeyCode.VcSpace);
        await session.KeyAsync(KeyCode.VcV, down: false);
        await session.WaitForStateAsync(state => state.PickerVisible, "picker kept open after Space");
        await session.ReleaseModifierAsync();
        await session.TapAsync(KeyCode.VcEscape);
        await session.WaitForStateAsync(state => !state.PickerVisible, "picker hidden after Escape");
        await session.WaitForIdleAsync();
        await Task.Delay(500, TestContext.Current.CancellationToken);

        Assert.Equal(string.Empty, await session.GetTargetTextAsync());
    }

    [Fact]
    public async Task TypingWithoutTheModifierIsNotIntercepted()
    {
        SkipUnlessEnabled();
        await session.PrepareTargetAsync();

        await session.TapAsync(KeyCode.VcV);
        await session.TapAsync(KeyCode.VcV);

        Assert.Equal("vv", await session.WaitForTargetTextAsync("vv"));
    }

    [Fact]
    public async Task PastingRestoresRichTextFormats()
    {
        SkipUnlessEnabled();
        var id = Unique();
        await session.CopyHtmlAsync($"Rich {id}", $"<b>Rich</b> <i>{id}</i>");
        await session.CopyTextAsync($"plain {id}");
        await session.PrepareTargetAsync();

        await session.PressModifierAsync();
        await session.HoldVUntilThePickerShowsAsync();
        await session.TapAsync(KeyCode.VcV);
        await session.WaitForStateAsync(state => state.PickerVisible && state.SelectedIndex == 1, "rich item selected");
        await session.ReleaseModifierAsync();

        Assert.Equal($"Rich {id}", await session.WaitForTargetTextAsync($"Rich {id}"));
        await session.WaitForIdleAsync();
        var formats = await session.GetClipboardFormatsAsync();
        Assert.Contains(formats, format => format.Contains("html", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PasteAsPlainTextDropsFormatting()
    {
        SkipUnlessEnabled();
        var id = Unique();
        await session.CopyHtmlAsync($"Styled {id}", $"<u>Styled</u> {id}");
        await session.CopyTextAsync($"newest {id}");
        await session.PrepareTargetAsync();

        await session.PressModifierAsync();
        await session.HoldVUntilThePickerShowsAsync();
        await session.TapAsync(KeyCode.VcV);
        await session.WaitForStateAsync(state => state.PickerVisible && state.SelectedIndex == 1, "styled item selected");
        // Actions: Paste, Paste as file, Paste as plain text.
        await session.TapAsync(KeyCode.VcRight);
        await session.TapAsync(KeyCode.VcDown);
        await session.TapAsync(KeyCode.VcDown);
        await Task.Delay(200, TestContext.Current.CancellationToken);
        await session.ReleaseModifierAsync();

        Assert.Equal($"Styled {id}", await session.WaitForTargetTextAsync($"Styled {id}"));
        await session.WaitForIdleAsync();
        var formats = await session.GetClipboardFormatsAsync();
        Assert.DoesNotContain(formats, format => format.Contains("html", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReleasingTheModifierBeforeVStillPastes()
    {
        SkipUnlessEnabled();
        await session.CopyTextAsync("rolling " + Unique());
        var expected = session.ReadState().Previews[0];
        await session.PrepareTargetAsync();

        // A "rolling" release: Ctrl comes up a moment before V.
        await session.PressModifierAsync();
        await session.KeyAsync(KeyCode.VcV, down: true, afterMilliseconds: 40);
        await session.ReleaseModifierAsync();
        await session.KeyAsync(KeyCode.VcV, down: false);

        Assert.Equal(expected, await session.WaitForTargetTextAsync(expected));
        await session.WaitForIdleAsync();
    }

    [Fact]
    public async Task ShortcutsWithAnotherModifierAreLeftToTheApplication()
    {
        SkipUnlessEnabled();
        await session.CopyTextAsync("unrelated " + Unique());
        await session.PrepareTargetAsync();
        await session.WaitForIdleAsync();
        var before = session.ReadState();

        await session.PressModifierAsync();
        await session.KeyAsync(KeyCode.VcLeftShift, down: true);
        await session.KeyAsync(KeyCode.VcV, down: true);
        await Task.Delay(600, TestContext.Current.CancellationToken);
        await session.KeyAsync(KeyCode.VcV, down: false);
        await session.KeyAsync(KeyCode.VcLeftShift, down: false);
        await session.ReleaseModifierAsync();
        await Task.Delay(500, TestContext.Current.CancellationToken);

        var after = session.ReadState();
        Assert.False(after.PickerVisible);
        Assert.Equal(before.EffectsProcessed, after.EffectsProcessed);
    }

    [Fact]
    public async Task PasswordManagerSecretsAreNotRecorded()
    {
        SkipUnlessEnabled();
        var id = Unique();
        await session.CopyTextAsync($"visible {id}");
        await session.CopySecretAsync($"hunter2 {id}");
        await Task.Delay(1500, TestContext.Current.CancellationToken);

        var state = session.ReadState();
        Assert.Equal($"visible {id}", state.Previews[0]);
        Assert.DoesNotContain(state.Previews, preview => preview.Contains("hunter2", StringComparison.Ordinal));
    }

    [Fact]
    public async Task S_SearchesTheHistory_AndPastesTheMatchIntoTheWindowTheUserCameFrom()
    {
        SkipUnlessEnabled();
        var id = Unique();
        await session.CopyTextAsync($"apple {id}");
        await session.CopyTextAsync($"banana {id}");
        await session.CopyTextAsync($"cherry {id}");
        await session.PrepareTargetAsync();

        // S starts a search; the S itself never reaches the target.
        await session.PressModifierAsync();
        await session.KeyAsync(KeyCode.VcV, down: true);
        await session.TapAsync(KeyCode.VcS);
        await session.KeyAsync(KeyCode.VcV, down: false);
        await session.ReleaseModifierAsync();
        await session.WaitForStateAsync(state => state.PickerVisible && state.Searching && state.SearchFocused, "the search box focused");

        // The picker has the focus: the typing goes to the search box, not to the target.
        await session.TypeAsync("banana " + id);
        await Task.Delay(300, TestContext.Current.CancellationToken);
        Assert.Equal(string.Empty, await session.GetTargetTextAsync());
        await session.TapAsync(KeyCode.VcEnter);

        await session.ExpectTargetTextAsync($"banana {id}");
        var state = await session.WaitForStateAsync(s => !s.PickerVisible && !s.Searching && s.Previews[0] == $"banana {id}", "picker hidden and the match moved to the top");
        Assert.Equal($"cherry {id}", state.Previews[1]);
        await session.WaitForIdleAsync();

        // The focus went back because Shapeshifter gave it back, not because hiding the picker happened to activate
        // the target (which made this test fail now and then on Windows).
        Assert.DoesNotContain(FocusNotRestored, session.Logs, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EscapeWhileSearching_GivesTheFocusBack_WithoutPasting()
    {
        SkipUnlessEnabled();
        var id = Unique();
        await session.CopyTextAsync($"first {id}");
        await session.CopyTextAsync($"second {id}");
        await session.PrepareTargetAsync();

        await session.PressModifierAsync();
        await session.KeyAsync(KeyCode.VcV, down: true);
        await session.TapAsync(KeyCode.VcS);
        await session.KeyAsync(KeyCode.VcV, down: false);
        await session.ReleaseModifierAsync();
        await session.WaitForStateAsync(state => state.Searching && state.SearchFocused, "the search box focused");
        await session.TypeAsync("first");
        await session.TapAsync(KeyCode.VcEscape);
        await session.WaitForStateAsync(state => !state.PickerVisible && !state.Searching, "picker hidden after Escape");
        await session.WaitForIdleAsync();

        // Nothing was pasted, and the target has the keyboard focus again.
        await session.TypeAsync("ok");
        Assert.Equal("ok", await session.WaitForTargetTextAsync("ok"));
        Assert.DoesNotContain(FocusNotRestored, session.Logs, StringComparison.Ordinal);
    }

    /// <summary>What Shapeshifter logs when it could not give the focus back after a search.</summary>
    private const string FocusNotRestored = "Could not give the keyboard focus back";

    private static void SkipUnlessEnabled()
    {
        if (!ShapeshifterSession.IsEnabled)
        {
            Assert.Skip(ShapeshifterSession.SkipReason);
        }
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..6];
}

using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Coaching;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Tests.Coaching;

public sealed class CopyCoachTests
{
    private readonly FakeTimeProvider time = new();
    private readonly IClipboardSourceLocator locator = Substitute.For<IClipboardSourceLocator>();
    private readonly CopyCoach coach;
    private bool enabled = true;

    public CopyCoachTests()
    {
        coach = new CopyCoach(locator, "Ctrl", () => enabled, time);
    }

    private static ClipboardItem Item(string text) => new(new ClipboardSnapshot { Text = text });

    private static ClipboardSource App(string name) => new(name, name + " - window", null);

    private CoachingTip? Copy(ClipboardItem item, string? from, bool isNew = true) =>
        coach.OnCopied(item, isNew, from is null ? null : App(from));

    private void PasteInto(string? application)
    {
        locator.GetCurrentSource().Returns(application is null ? null : App(application));
        coach.OnPasted();
    }

    // ------------------------------------------------------------ copied again (#284)

    [Fact]
    public void CopyingSomethingAlreadyInTheHistory_SuggestsPickingItFromTheHistory()
    {
        var first = Item("first");
        Copy(first, "Editor");
        Copy(Item("second"), "Editor");

        var tip = Copy(first, "Editor", isNew: false);

        Assert.NotNull(tip);
        Assert.Equal(CoachingTipKind.CopiedAgain, tip.Kind);
        Assert.Contains("Ctrl+V", tip.Message);
    }

    [Fact]
    public void CopyingTheCurrentClipboardAgain_IsNoHabitToCoach()
    {
        var item = Item("same");
        Copy(item, "Editor");

        Assert.Null(Copy(item, "Editor", isNew: false));
    }

    [Fact]
    public void CopyingWhatWasOnTheClipboardAtStartup_IsNoHabitToCoach()
    {
        var item = Item("from before");
        coach.OnStartupCapture(item);

        Assert.Null(Copy(item, "Editor", isNew: false));
    }

    [Fact]
    public void ANewCopy_IsNoTip()
    {
        Assert.Null(Copy(Item("a"), "Editor"));
        Assert.Null(Copy(Item("b"), "Editor"));
    }

    [Fact]
    public void TheMessageUsesThePlatformsModifier()
    {
        var mac = new CopyCoach(locator, "Cmd", time: time);
        var first = Item("first");
        mac.OnCopied(first, true, null);
        mac.OnCopied(Item("second"), true, null);

        var tip = mac.OnCopied(first, false, null);

        Assert.Contains("Cmd+V", tip!.Message);
    }

    [Fact]
    public void TheMessageFollowsAChangedShortcut()
    {
        var modifier = "Ctrl";
        var changing = new CopyCoach(locator, () => modifier, time: time);
        var first = Item("first");
        changing.OnCopied(first, true, null);
        changing.OnCopied(Item("second"), true, null);
        modifier = "Win";

        var tip = changing.OnCopied(first, false, null);

        Assert.Contains("Win+V", tip!.Message);
    }

    // ------------------------------------------------------------ back and forth (#310)

    [Fact]
    public void CopyPasteElsewhereAndCopyAgainFromTheSameApplication_SuggestsCopyingEverythingFirst()
    {
        Copy(Item("a"), "Photoshop");
        PasteInto("Paint");
        time.Advance(TimeSpan.FromSeconds(20));

        var tip = Copy(Item("b"), "Photoshop");

        Assert.NotNull(tip);
        Assert.Equal(CoachingTipKind.CopiedBackAndForth, tip.Kind);
        Assert.Contains("Photoshop", tip.Message);
        Assert.Contains("Paint", tip.Message);
        Assert.Contains("Ctrl+V", tip.Message);
    }

    [Fact]
    public void ApplicationsAreComparedIgnoringCase()
    {
        Copy(Item("a"), "Photoshop");
        PasteInto("Paint");

        Assert.NotNull(Copy(Item("b"), "PHOTOSHOP"));
    }

    [Fact]
    public void ComingBackAfterThirtySecondsFromTheFirstCopy_IsNoTip()
    {
        Copy(Item("a"), "Photoshop");
        time.Advance(TimeSpan.FromSeconds(25));
        PasteInto("Paint");
        time.Advance(TimeSpan.FromSeconds(6));

        Assert.Null(Copy(Item("b"), "Photoshop"));
    }

    [Fact]
    public void ComingBackExactlyAtThirtySeconds_StillCounts()
    {
        Copy(Item("a"), "Photoshop");
        PasteInto("Paint");
        time.Advance(CopyCoach.BackAndForthWindow);

        Assert.NotNull(Copy(Item("b"), "Photoshop"));
    }

    [Fact]
    public void PastingIntoTheSameApplication_IsNoTip()
    {
        Copy(Item("a"), "Photoshop");
        PasteInto("Photoshop");

        Assert.Null(Copy(Item("b"), "Photoshop"));
    }

    [Fact]
    public void CopyingFromAThirdApplication_IsNoTip()
    {
        Copy(Item("a"), "Photoshop");
        PasteInto("Paint");

        Assert.Null(Copy(Item("b"), "Word"));
    }

    [Fact]
    public void WithoutAPaste_IsNoTip()
    {
        Copy(Item("a"), "Photoshop");

        Assert.Null(Copy(Item("b"), "Photoshop"));
    }

    [Fact]
    public void AnotherCopyInBetween_EndsTheRoundTrip()
    {
        Copy(Item("a"), "Photoshop");
        PasteInto("Paint");
        Copy(Item("x"), "Word");

        Assert.Null(Copy(Item("b"), "Photoshop"));
    }

    [Fact]
    public void OnlyTheFirstReturn_GetsTheTip()
    {
        coach.Cooldown = TimeSpan.Zero;
        Copy(Item("a"), "Photoshop");
        PasteInto("Paint");
        Assert.NotNull(Copy(Item("b"), "Photoshop"));

        Assert.Null(Copy(Item("c"), "Photoshop"));
    }

    [Theory]
    [InlineData(null, "Paint")]
    [InlineData("Photoshop", null)]
    [InlineData("  ", "Paint")]
    public void UnknownApplications_AreNoTip(string? from, string? into)
    {
        Copy(Item("a"), from);
        PasteInto(into);

        Assert.Null(Copy(Item("b"), from));
    }

    [Fact]
    public void UsingThePicker_EndsTheRoundTrip()
    {
        Copy(Item("a"), "Photoshop");
        PasteInto("Paint");
        coach.OnPickerUsed();

        Assert.Null(Copy(Item("b"), "Photoshop"));
    }

    [Fact]
    public void APasteAfterThePickerWasShown_DoesNotStartARoundTrip()
    {
        Copy(Item("a"), "Photoshop");
        coach.OnPickerUsed();
        PasteInto("Paint");

        Assert.Null(Copy(Item("b"), "Photoshop"));
    }

    [Fact]
    public void ALocatorThatThrows_IsNoTip()
    {
        Copy(Item("a"), "Photoshop");
        locator.GetCurrentSource().Returns(_ => throw new InvalidOperationException("boom"));
        coach.OnPasted();

        Assert.Null(Copy(Item("b"), "Photoshop"));
    }

    // ------------------------------------------------------------ restraint

    [Fact]
    public void TipsAreNotRepeatedWithinTheCooldown()
    {
        var first = Item("first");
        Copy(first, "Editor");
        Copy(Item("second"), "Editor");
        Assert.NotNull(Copy(first, "Editor", isNew: false));

        time.Advance(coach.Cooldown - TimeSpan.FromSeconds(1));
        Copy(Item("third"), "Editor");
        Assert.Null(Copy(first, "Editor", isNew: false));

        time.Advance(TimeSpan.FromSeconds(1));
        Copy(Item("fourth"), "Editor");
        Assert.NotNull(Copy(first, "Editor", isNew: false));
    }

    [Fact]
    public void WhenTurnedOff_NoTips()
    {
        enabled = false;
        var first = Item("first");
        Copy(first, "Photoshop");
        PasteInto("Paint");
        Assert.Null(Copy(Item("second"), "Photoshop"));

        Assert.Null(Copy(first, "Photoshop", isNew: false));
    }

    [Fact]
    public void ATipSuppressedWhileTurnedOff_DoesNotStartTheCooldown()
    {
        enabled = false;
        var first = Item("first");
        Copy(first, "Editor");
        Copy(Item("second"), "Editor");
        Assert.Null(Copy(first, "Editor", isNew: false));

        enabled = true;
        Copy(Item("third"), "Editor");
        Assert.NotNull(Copy(first, "Editor", isNew: false));
    }
}

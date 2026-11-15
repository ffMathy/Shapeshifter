using Shapeshifter.Core.Input;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Tests.Input;

/// <summary>Drives a <see cref="PasteGestureStateMachine"/> with a fake clock and a configurable history.</summary>
internal sealed class GestureHarness
{
    public GestureHarness(IReadOnlyList<GestureItem> items, int defaultIndex = 0, TimeSpan? showDelay = null)
    {
        Items = [.. items];
        DefaultIndex = defaultIndex;
        Machine = new PasteGestureStateMachine(() =>
        {
            SessionCount++;
            return (Items, DefaultIndex);
        });
        if (showDelay is { } delay)
        {
            Machine.ShowDelay = delay;
        }
    }

    public PasteGestureStateMachine Machine { get; }

    public List<GestureItem> Items { get; set; }

    public int DefaultIndex { get; set; }

    public int SessionCount { get; private set; }

    public TimeSpan Now { get; set; } = TimeSpan.FromSeconds(10);

    public static GestureHarness WithItems(int count, int actionCount = 3, int defaultIndex = 0, TimeSpan? showDelay = null) =>
        new(CreateItems(count, actionCount), defaultIndex, showDelay);

    public static List<GestureItem> CreateItems(int count, int actionCount = 3) =>
        [.. Enumerable.Range(0, count).Select(i => new GestureItem(IdFor(i), actionCount))];

    /// <summary>Deterministic, readable ids: 0000000N-0000-0000-0000-000000000000 (N = index + 1).</summary>
    public static Guid IdFor(int index) => new(index + 1, 0, 0, new byte[8]);

    public GestureResult Down(GestureKey key) => Machine.Handle(new KeyInput(key, true), Now);

    public GestureResult Up(GestureKey key) => Machine.Handle(new KeyInput(key, false), Now);

    /// <summary>Key down followed by key up; returns the result of the key down.</summary>
    public GestureResult Press(GestureKey key)
    {
        var down = Down(key);
        Up(key);
        return down;
    }

    public GestureResult Advance(TimeSpan by)
    {
        Now += by;
        return Machine.Tick(Now);
    }

    /// <summary>Modifier down, V down, V up: the gesture is active; the picker is shown only if ShowDelay is zero.</summary>
    public GestureResult Start()
    {
        Down(GestureKey.Modifier);
        var result = Down(GestureKey.Trigger);
        Up(GestureKey.Trigger);
        return result;
    }

    /// <summary>Starts the gesture and waits past the show delay so the picker is visible.</summary>
    public void StartAndShow()
    {
        Start();
        Advance(Machine.ShowDelay + TimeSpan.FromMilliseconds(1));
        Assert.True(Machine.IsPickerVisible);
    }
}

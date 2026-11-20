using SharpHook.Data;

namespace Shapeshifter.EndToEnd.Tests;

/// <summary>
/// Images copied the way real Windows applications copy them must come back from the history pixel for pixel,
/// in every format applications read: CF_DIB, CF_DIBV5 and PNG.
/// </summary>
[Collection(nameof(EndToEndCollection))]
public sealed class ImageEndToEndTests(ShapeshifterSession session)
{
    /// <summary>The target application's test image (ARGB, rows top to bottom), formatted as the target reports pixels.</summary>
    private const string ExpectedPixels =
        "FFFF0000,FF00FF00,FF0000FF,FFFFFFFF,FF000000,FFFFFF00,FF00FFFF,FFFF00FF,FF808080,FFFF8000,FF123456,FFFEDCBA";

    /// <summary>
    /// Issues #407 (screenshots could no longer be pasted into websites) and #440 (screenshots pasted white into
    /// Telegram): a screenshot (24-bit CF_DIB plus PNG) picked from the history must be restored with the same
    /// opaque pixels in CF_DIB, CF_DIBV5 and PNG.
    /// </summary>
    [Fact]
    public async Task ScreenshotIsRestoredPixelForPixel()
    {
        SkipUnlessWindows();
        var id = Unique();
        await session.CopyTextAsync($"before {id}");
        var captured = await session.CopyImageAsync("screenshot");
        Assert.Equal("Image", captured.Kinds[0]);
        var original = await session.DescribeImageAsync();

        await PasteSecondItemAsync(id);

        var restored = await session.DescribeImageAsync();
        var report = Report(original, restored);
        foreach (var format in new[] { "CF_DIB", "CF_DIBV5", "PNG" })
        {
            Assert.True(Pixels(original[format]) == ExpectedPixels, $"The test setup did not produce the expected {format}.\n{report}");
            Assert.True(Pixels(restored[format]) == ExpectedPixels, $"{format} was not restored pixel for pixel.\n{report}");
        }
    }

    /// <summary>
    /// Issue #606: Firefox's "Copy Image" puts the image (CF_DIB) and a temporary file (CF_HDROP) on the clipboard,
    /// and deletes the file later. The item must show as an image and paste the image after the file is gone.
    /// </summary>
    [Fact]
    public async Task FirefoxImageIsAnImageAndOutlivesItsTemporaryFile()
    {
        SkipUnlessWindows();
        var id = Unique();
        await session.CopyTextAsync($"before {id}");
        var captured = await session.CopyImageAsync("firefox");
        await session.DeleteTemporaryImageAsync();

        await PasteSecondItemAsync(id);

        var restored = await session.DescribeImageAsync();
        var report = Report(new Dictionary<string, string>(), restored);
        Assert.True(captured.Kinds[0] == "Image", $"The item was captured as {captured.Kinds[0]} ('{captured.Previews[0]}'), not as an image.\n{report}");
        foreach (var format in new[] { "CF_DIB", "CF_DIBV5", "PNG" })
        {
            Assert.True(Pixels(restored[format]) == ExpectedPixels, $"{format} was not restored pixel for pixel.\n{report}");
        }
    }

    /// <summary>
    /// An image picked from the history must survive another application reading it. On Linux, Shapeshifter read its
    /// own clipboard write back and disposed the bitmap it was still serving, so it crashed as soon as an application
    /// asked for the image.
    /// </summary>
    [Fact]
    public async Task AnImageFromTheHistoryCanBeReadByAnotherApplication()
    {
        SkipUnlessEnabled();
        var id = Unique();
        await session.CopyTextAsync($"before {id}");
        await session.CopyBitmapAsync();

        await PasteSecondItemAsync(id);

        Assert.Equal("IMAGE 4x3", await session.ReadImageAsync());
        Assert.Equal("IMAGE 4x3", await session.ReadImageAsync());
        await Task.Delay(500, TestContext.Current.CancellationToken);
        Assert.True(session.IsShapeshifterRunning, $"Shapeshifter exited after the image was read.\n{session.Logs}");
        await session.CopyTextAsync($"still running {id}");
    }

    /// <summary>Copies one more text item, then picks the image (now second) with holding Ctrl+V and pressing V again, and pastes it.</summary>
    private async Task PasteSecondItemAsync(string id)
    {
        await session.CopyTextAsync($"after {id}");
        await session.PrepareTargetAsync();
        await session.PressModifierAsync();
        await session.HoldVUntilThePickerShowsAsync();
        await session.TapAsync(KeyCode.VcV);
        await session.WaitForStateAsync(state => state.PickerVisible && state.SelectedIndex == 1, "image item selected");
        await session.ReleaseModifierAsync();
        await session.WaitForStateAsync(state => !state.PickerVisible && state.Kinds[0] != "Text", "image item pasted and moved to the top");
        await session.WaitForIdleAsync();
    }

    private static string Pixels(string description)
    {
        var index = description.IndexOf("pixels=", StringComparison.Ordinal);
        return index < 0 ? description : description[(index + 7)..];
    }

    private static string Report(IReadOnlyDictionary<string, string> original, IReadOnlyDictionary<string, string> restored) =>
        "Original clipboard:\n" + string.Join('\n', original.Select(pair => $"  {pair.Key}: {pair.Value}"))
        + "\nAfter pasting from Shapeshifter:\n" + string.Join('\n', restored.Select(pair => $"  {pair.Key}: {pair.Value}"));

    private static void SkipUnlessEnabled()
    {
        if (!ShapeshifterSession.IsEnabled)
        {
            Assert.Skip(ShapeshifterSession.SkipReason);
        }
    }

    private static void SkipUnlessWindows()
    {
        if (!ShapeshifterSession.IsEnabled)
        {
            Assert.Skip(ShapeshifterSession.SkipReason);
        }

        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip("These tests put native Windows clipboard formats on the clipboard.");
        }
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..6];
}

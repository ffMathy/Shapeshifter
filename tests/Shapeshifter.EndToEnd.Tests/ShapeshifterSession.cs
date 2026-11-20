using System.Text;
using System.Text.Json;
using SharpHook.Data;
using SharpHook.Simulation;

namespace Shapeshifter.EndToEnd.Tests;

/// <summary>Snapshot of the diagnostics state file that Shapeshifter writes.</summary>
internal sealed record AppState(int HistoryCount, IReadOnlyList<string> Previews, bool PickerVisible, int SelectedIndex, int EffectsProcessed)
{
    /// <summary>The kind of each item (Text, Image, File, ...), in the same order as <see cref="Previews"/>.</summary>
    public IReadOnlyList<string> Kinds { get; init; } = [];

    /// <summary>The picker shows the search box.</summary>
    public bool Searching { get; init; }

    /// <summary>The picker has the keyboard focus for the search.</summary>
    public bool SearchFocused { get; init; }
}

/// <summary>
/// One running Shapeshifter (with isolated settings and history) plus one target application, shared by the tests
/// in <see cref="EndToEndCollection"/>.
/// </summary>
public sealed class ShapeshifterSession : IAsyncLifetime
{
    public const string EnableVariable = "SHAPESHIFTER_E2E";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(60);

    /// <summary>When set, the session's settings, history and logs are kept in this folder (for CI artifacts).</summary>
    public const string ArtifactsVariable = "SHAPESHIFTER_E2E_ARTIFACTS";

    private readonly string home = Path.Combine(
        Environment.GetEnvironmentVariable(ArtifactsVariable) is { Length: > 0 } artifacts ? artifacts : Path.GetTempPath(),
        "shapeshifter-e2e-" + Guid.NewGuid().ToString("N")[..8]);
    private TestProcess? shapeshifter;
    private TestProcess? target;
    private EventSimulator? simulator;

    public static bool IsEnabled => Environment.GetEnvironmentVariable(EnableVariable) == "1";

    public static string SkipReason => $"End-to-end tests need a desktop session; set {EnableVariable}=1 to run them.";

    private string StateFile => Path.Combine(home, "state.json");

    /// <summary>Ctrl, or Cmd on macOS.</summary>
    private static KeyCode Modifier => OperatingSystem.IsMacOS() ? KeyCode.VcLeftMeta : KeyCode.VcLeftControl;

    public string Logs => $"--- Shapeshifter ---\n{shapeshifter?.Log}\n--- Target ---\n{target?.Log}";

    public async ValueTask InitializeAsync()
    {
        if (!IsEnabled)
        {
            return;
        }

        Directory.CreateDirectory(home);
        var environment = new Dictionary<string, string>
        {
            ["SHAPESHIFTER_HOME"] = home,
            ["SHAPESHIFTER_STATE_FILE"] = StateFile,
        };

        target = TestProcess.Start(TestProcess.GetAssemblyPath("TargetAppAssembly"), new Dictionary<string, string>());
        await target.WaitForLineAsync(line => line == "TARGET_READY", StartupTimeout);

        shapeshifter = TestProcess.Start(TestProcess.GetAssemblyPath("ShapeshifterAssembly"), environment);
        await shapeshifter.WaitForLineAsync(line => line == "SHAPESHIFTER_READY", StartupTimeout);
        simulator = EventSimulator.Create("Shapeshifter end-to-end tests");
    }

    public async ValueTask DisposeAsync()
    {
        simulator?.Dispose();
        if (target is not null)
        {
            await target.DisposeAsync();
        }

        if (shapeshifter is not null)
        {
            await shapeshifter.DisposeAsync();
        }

        if (Environment.GetEnvironmentVariable(ArtifactsVariable) is { Length: > 0 })
        {
            await File.WriteAllTextAsync(Path.Combine(home, "processes.log"), Logs);
            return;
        }

        try
        {
            Directory.Delete(home, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    /// <summary>Copies plain text from the target application and waits until Shapeshifter captured it.</summary>
    public async Task CopyTextAsync(string text)
    {
        var before = ReadState();
        await ExpectOkAsync($"copy-text {Encode(text)}");
        await WaitForCaptureAsync(TextPreview(text), before);
    }

    /// <summary>Copies text with an HTML version from the target application and waits for the capture.</summary>
    public async Task CopyHtmlAsync(string text, string html)
    {
        var before = ReadState();
        await ExpectOkAsync($"copy-html {Encode(text)} {Encode(html)}");
        await WaitForCaptureAsync(TextPreview(text), before);
    }

    /// <summary>Copies text marked as sensitive (as password managers do) without waiting for a capture.</summary>
    public async Task CopySecretAsync(string text) => await ExpectOkAsync($"copy-secret {Encode(text)}");

    /// <summary>
    /// Puts an image on the clipboard the way a real application does (Windows only; see the target's
    /// <c>copy-image</c> command) and waits until Shapeshifter captured something that is not text.
    /// </summary>
    internal async Task<AppState> CopyImageAsync(string scenario)
    {
        await ExpectOkAsync($"copy-image {scenario}");
        return await WaitForStateAsync(state => state.Kinds.Count > 0 && state.Kinds[0] != "Text", $"capture of the '{scenario}' image");
    }

    /// <summary>Puts the target's test image on the clipboard as a bitmap and waits until Shapeshifter captured an image.</summary>
    internal async Task<AppState> CopyBitmapAsync()
    {
        await ExpectOkAsync("copy-bitmap");
        return await WaitForStateAsync(state => state.Kinds.Count > 0 && state.Kinds[0] == "Image", "capture of the bitmap");
    }

    /// <summary>Has the target read the clipboard's image, as an application pasting it does: <c>IMAGE WxH</c> or <c>NONE</c>.</summary>
    public async Task<string> ReadImageAsync() => await target!.SendAsync("read-image");

    /// <summary>Whether Shapeshifter is still running.</summary>
    public bool IsShapeshifterRunning => shapeshifter is { HasExited: false };

    /// <summary>Deletes the temporary file of the <c>firefox</c> image scenario.</summary>
    public async Task DeleteTemporaryImageAsync() => await ExpectOkAsync("delete-temp");

    /// <summary>Every format on the clipboard, and the pixels of CF_DIB, CF_DIBV5 and PNG, keyed by format.</summary>
    public async Task<IReadOnlyDictionary<string, string>> DescribeImageAsync()
    {
        var response = await target!.SendAsync("describe-image");
        if (!response.StartsWith("IMAGE ", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(response);
        }

        return response[6..].Split(';').Select(part => part.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);
    }

    /// <summary>Focuses the target's text box and clears it.</summary>
    public async Task PrepareTargetAsync()
    {
        await ExpectOkAsync("clear");
        await ExpectOkAsync("focus");
        await Task.Delay(200);
    }

    public async Task<string> GetTargetTextAsync()
    {
        var response = await target!.SendAsync("get");
        return response.StartsWith("TEXT ", StringComparison.Ordinal)
            ? Encoding.UTF8.GetString(Convert.FromBase64String(response[5..]))
            : throw new InvalidOperationException(response);
    }

    public async Task<IReadOnlyList<string>> GetClipboardFormatsAsync()
    {
        var response = await target!.SendAsync("formats");
        return response.StartsWith("FORMATS ", StringComparison.Ordinal)
            ? response[8..].Split('|', StringSplitOptions.RemoveEmptyEntries)
            : throw new InvalidOperationException(response);
    }

    /// <summary>Waits until the target's text equals <paramref name="expected"/> and stays that way briefly.</summary>
    public async Task<string> WaitForTargetTextAsync(string expected, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        string text;
        do
        {
            text = await GetTargetTextAsync();
            if (text == expected)
            {
                // Make sure nothing else arrives afterwards (for example an unsuppressed native paste).
                await Task.Delay(500);
                return await GetTargetTextAsync();
            }

            await Task.Delay(100);
        }
        while (DateTime.UtcNow < deadline);

        return text;
    }

    /// <summary>
    /// Like <see cref="WaitForTargetTextAsync"/>, but fails with both processes' logs and Shapeshifter's state when the
    /// text does not arrive, so a CI run shows why.
    /// </summary>
    public async Task ExpectTargetTextAsync(string expected, TimeSpan? timeout = null)
    {
        var text = await WaitForTargetTextAsync(expected, timeout);
        if (text != expected)
        {
            throw new Xunit.Sdk.XunitException(
                $"The target's text is \"{text}\", expected \"{expected}\". Last state: {ReadState()}\n{Logs}");
        }
    }

    internal AppState ReadState()
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                // Share delete access, so Shapeshifter can replace the file while it is being read (Windows).
                using var stream = new FileStream(StateFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var document = JsonDocument.Parse(stream);
                var root = document.RootElement;
                return new AppState(
                    root.GetProperty("historyCount").GetInt32(),
                    root.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("preview").GetString() ?? string.Empty).ToArray(),
                    root.GetProperty("pickerVisible").GetBoolean(),
                    root.GetProperty("selectedIndex").GetInt32(),
                    root.GetProperty("effectsProcessed").GetInt32())
                {
                    Kinds = root.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("kind").GetString() ?? string.Empty).ToArray(),
                    Searching = root.GetProperty("searching").GetBoolean(),
                    SearchFocused = root.GetProperty("searchFocused").GetBoolean(),
                };
            }
            catch (Exception exception) when ((exception is IOException or JsonException or KeyNotFoundException) && attempt < 50)
            {
                Thread.Sleep(20);
            }
        }
    }

    internal async Task<AppState> WaitForStateAsync(Func<AppState, bool> predicate, string description, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        AppState state;
        do
        {
            state = ReadState();
            if (predicate(state))
            {
                return state;
            }

            await Task.Delay(50);
        }
        while (DateTime.UtcNow < deadline);

        throw new TimeoutException($"Timed out waiting for: {description}. Last state: {state}\n{Logs}");
    }

    /// <summary>Waits until all queued gesture effects (paste, hide, ...) have been processed.</summary>
    internal async Task WaitForIdleAsync()
    {
        var previous = -1;
        for (var stable = 0; stable < 4;)
        {
            var current = ReadState().EffectsProcessed;
            stable = current == previous ? stable + 1 : 0;
            previous = current;
            await Task.Delay(100);
        }
    }

    public async Task PressModifierAsync() => await KeyAsync(Modifier, down: true);

    public async Task ReleaseModifierAsync() => await KeyAsync(Modifier, down: false);

    /// <summary>
    /// With the modifier held, holds V until the picker shows and releases it. A quick V would paste instead (issue #639).
    /// </summary>
    public async Task HoldVUntilThePickerShowsAsync()
    {
        await KeyAsync(KeyCode.VcV, down: true);
        await WaitForStateAsync(state => state.PickerVisible, "picker after holding V");
        await KeyAsync(KeyCode.VcV, down: false);
    }

    public async Task TapAsync(KeyCode key, int holdMilliseconds = 30)
    {
        await KeyAsync(key, down: true, holdMilliseconds);
        await KeyAsync(key, down: false);
    }

    /// <summary>Types lower-case letters, digits and spaces with real key presses.</summary>
    public async Task TypeAsync(string text)
    {
        foreach (var character in text)
        {
            var key = character switch
            {
                ' ' => KeyCode.VcSpace,
                >= 'a' and <= 'z' => Enum.Parse<KeyCode>("Vc" + char.ToUpperInvariant(character)),
                >= '0' and <= '9' => Enum.Parse<KeyCode>("Vc" + character),
                _ => throw new ArgumentException($"Cannot type '{character}'.", nameof(text)),
            };
            await TapAsync(key);
        }
    }

    public async Task KeyAsync(KeyCode key, bool down, int afterMilliseconds = 60)
    {
        var result = down ? simulator!.SimulateKeyPress(key) : simulator!.SimulateKeyRelease(key);
        if (result != UioHookResult.Success)
        {
            throw new InvalidOperationException($"Simulating {key} failed: {result}");
        }

        await Task.Delay(afterMilliseconds);
    }

    private async Task WaitForCaptureAsync(string preview, AppState before) =>
        await WaitForStateAsync(
            state => state.Previews.Count > 0 && state.Previews[0] == preview && (state.HistoryCount != before.HistoryCount || (before.Previews.Count == 0 || before.Previews[0] != preview)),
            $"capture of '{preview}'");

    private async Task ExpectOkAsync(string command)
    {
        var response = await target!.SendAsync(command);
        if (response != "OK")
        {
            throw new InvalidOperationException($"'{command}' failed: {response}");
        }
    }

    private static string TextPreview(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}

[CollectionDefinition(nameof(EndToEndCollection), DisableParallelization = true)]
public sealed class EndToEndCollection : ICollectionFixture<ShapeshifterSession>
{
}

using System.Diagnostics;
using System.Text;
using SharpHook.Data;

namespace Shapeshifter.EndToEnd.Tests;

/// <summary>
/// Issue #441: pasting with Ctrl+V into a Remote Desktop window while Shapeshifter runs on the local computer.
/// </summary>
/// <remarks>
/// Needs a prepared Windows machine (see <c>.github/workflows/remote-desktop.yml</c>): Remote Desktop enabled, a
/// second user whose logon starts the target application with <c>--mirror</c>, and saved credentials for
/// <see cref="Host"/>. The test connects with <c>mstsc</c>, so the Remote Desktop window has the focus, copies on the
/// local computer and presses the keys locally, exactly like a user would, then reads what arrived in the remote
/// session from the mirror file. Typing plain letters first checks that keystrokes reach the session at all.
/// </remarks>
[Collection(nameof(EndToEndCollection))]
public sealed class RemoteDesktopEndToEndTests(ShapeshifterSession session)
{
    /// <summary>The mirror file the target application inside the remote session writes.</summary>
    public const string MirrorVariable = "SHAPESHIFTER_E2E_RDP_MIRROR";

    /// <summary>A second loopback address: mstsc refuses to connect to 127.0.0.1 and localhost.</summary>
    private const string Host = "127.0.0.2";

    private static readonly TimeSpan LogonTimeout = TimeSpan.FromMinutes(4);

    [Fact]
    public async Task PastingIntoARemoteDesktopSessionWorks()
    {
        if (!ShapeshifterSession.IsEnabled || !OperatingSystem.IsWindows()
            || Environment.GetEnvironmentVariable(MirrorVariable) is not { Length: > 0 } mirror)
        {
            Assert.Skip($"Needs Windows with a prepared Remote Desktop user; set {MirrorVariable} (see .github/workflows/remote-desktop.yml).");
            return;
        }

        var id = Guid.NewGuid().ToString("N")[..6];
        await session.CopyTextAsync($"older {id}");
        await session.CopyTextAsync($"newer {id}");

        using var client = Process.Start(new ProcessStartInfo("mstsc.exe", $"/v:{Host} /w:1280 /h:800") { UseShellExecute = true })!;
        await WaitForFileAsync(mirror + ".ready", LogonTimeout);
        await Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        var report = new StringBuilder();
        var expected = string.Empty;

        // 1. Plain typing: checks the harness (focus, input forwarding, mirror file) before testing anything else.
        foreach (var key in new[] { KeyCode.VcA, KeyCode.VcB, KeyCode.VcC })
        {
            await session.TapAsync(key);
        }

        expected += "abc";
        var typed = await WaitForMirrorAsync(mirror, expected);
        report.AppendLine($"Typing 'abc': remote text is '{typed}'");
        Assert.True(typed == expected, "Keystrokes do not reach the Remote Desktop session, so the test setup does not work.\n" + report + session.Logs);

        // 2. A quick Ctrl+V: Shapeshifter swallows the keystroke and replays the paste.
        await session.PressModifierAsync();
        await session.TapAsync(KeyCode.VcV);
        await session.ReleaseModifierAsync();
        expected += $"newer {id}";
        var quick = await WaitForMirrorAsync(mirror, expected);
        report.AppendLine($"Quick Ctrl+V: remote text is '{quick}' (expected '{expected}')");
        await session.WaitForIdleAsync();

        // 3. Picking the older item: Shapeshifter changes the clipboard right before it pastes, and the Remote
        //    Desktop client has to forward that change to the session in time.
        await session.PressModifierAsync();
        await session.HoldVUntilThePickerShowsAsync();
        await session.TapAsync(KeyCode.VcV);
        await session.WaitForStateAsync(state => state.PickerVisible && state.SelectedIndex == 1, "older item selected");
        await session.ReleaseModifierAsync();
        expected = quick + $"older {id}";
        var picked = await WaitForMirrorAsync(mirror, expected);
        report.AppendLine($"Ctrl+V, V (older item): remote text is '{picked}' (expected '{expected}')");
        await session.WaitForIdleAsync();

        TestContext.Current.SendDiagnosticMessage(report.ToString());
        Assert.True(quick.EndsWith($"newer {id}", StringComparison.Ordinal) && picked == expected, report.ToString());
    }

    private static async Task WaitForFileAsync(string path, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!File.Exists(path))
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"The target application did not start in the Remote Desktop session ({path} missing).");
            }

            await Task.Delay(500);
        }
    }

    /// <summary>Waits until the mirrored text equals <paramref name="expected"/> (and stays so briefly), or times out.</summary>
    private static async Task<string> WaitForMirrorAsync(string path, string expected)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        string text;
        do
        {
            text = await ReadMirrorAsync(path);
            if (text == expected)
            {
                await Task.Delay(700);
                return await ReadMirrorAsync(path);
            }

            await Task.Delay(100);
        }
        while (DateTime.UtcNow < deadline);

        return text;
    }

    private static async Task<string> ReadMirrorAsync(string path)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await File.ReadAllTextAsync(path);
            }
            catch (IOException) when (attempt < 20)
            {
                await Task.Delay(25);
            }
        }
    }
}

using System.Text.Json;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Engine;

namespace Shapeshifter.Desktop.Services;

/// <summary>
/// When <c>SHAPESHIFTER_STATE_FILE</c> is set, keeps a small JSON description of the app state in that file.
/// End-to-end tests use it to wait for captures and for the picker deterministically instead of sleeping.
/// </summary>
internal sealed class DiagnosticsStateWriter
{
    public const string Variable = "SHAPESHIFTER_STATE_FILE";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string path;
    private readonly ClipboardHistory history;
    private readonly PickerPresenter picker;
    private readonly Lock gate = new();
    private int effectsProcessed;

    private DiagnosticsStateWriter(string path, ClipboardHistory history, PickerPresenter picker)
    {
        this.path = path;
        this.history = history;
        this.picker = picker;
    }

    public static DiagnosticsStateWriter? TryCreate(ClipboardHistory history, PickerPresenter picker, ShapeshifterEngine engine)
    {
        var path = Environment.GetEnvironmentVariable(Variable);
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var writer = new DiagnosticsStateWriter(path, history, picker);
        history.Changed += (_, _) => writer.Write();
        picker.VisibilityChanged += (_, _) => writer.Write();
        engine.EffectProcessed += (_, _) =>
        {
            Interlocked.Increment(ref writer.effectsProcessed);
            writer.Write();
        };
        writer.Write();
        return writer;
    }

    public void Write()
    {
        var items = history.Items;
        var state = new
        {
            ready = true,
            historyCount = items.Count,
            items = items.Select(item => new { kind = item.Kind.ToString(), preview = item.PreviewText, pinned = item.IsPinned }),
            pickerVisible = picker.IsVisible,
            selectedIndex = picker.SelectedIndex,
            effectsProcessed = Volatile.Read(ref effectsProcessed),
        };

        lock (gate)
        {
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(state, JsonOptions));
            File.Move(temporary, path, overwrite: true);
        }
    }
}

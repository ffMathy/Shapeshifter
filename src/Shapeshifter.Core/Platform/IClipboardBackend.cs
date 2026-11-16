using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Platform;

/// <summary>Reads and writes the operating system clipboard.</summary>
public interface IClipboardBackend
{
    /// <summary>Reads everything currently on the clipboard. Returns <see cref="ClipboardSnapshot.Empty"/> when it is empty.</summary>
    Task<ClipboardSnapshot> ReadAsync(CancellationToken cancellationToken = default);

    /// <summary>Replaces the clipboard contents with <paramref name="snapshot"/>.</summary>
    Task WriteAsync(ClipboardSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// A cheap value that changes whenever the clipboard changes. Used for polling where the OS offers no
    /// notification. It is only compared for equality, so it can be the content itself or a hash of it.
    /// </summary>
    Task<string> GetChangeTokenAsync(CancellationToken cancellationToken = default);
}

/// <summary>Raises <see cref="Changed"/> when another application (or this one) changes the clipboard.</summary>
public interface IClipboardChangeSource : IDisposable
{
    event EventHandler? Changed;

    void Start();
}

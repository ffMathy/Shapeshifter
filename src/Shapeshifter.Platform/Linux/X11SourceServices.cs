using Serilog;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Platform.Linux;

/// <summary>The X11 window that is active when the clipboard changes.</summary>
internal sealed class X11ClipboardSourceLocator(ILogger logger) : IClipboardSourceLocator
{
    private readonly ILogger logger = logger;

    public ClipboardSource? GetCurrentSource() => null;
}

/// <summary>File and folder icons from the freedesktop icon theme.</summary>
internal sealed class FreedesktopFileIconProvider(ILogger logger) : IFileIconProvider
{
    private readonly ILogger logger = logger;

    public byte[]? GetIconPng(string path, int size) => null;
}

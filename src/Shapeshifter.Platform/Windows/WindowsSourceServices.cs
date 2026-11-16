using System.Runtime.Versioning;
using Serilog;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Platform.Windows;

/// <summary>The application (and window) that owns the clipboard or is in the foreground.</summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsClipboardSourceLocator(ILogger logger) : IClipboardSourceLocator
{
    private readonly ILogger logger = logger;

    public ClipboardSource? GetCurrentSource() => null;
}

/// <summary>Shell icons for files and folders, as Explorer shows them.</summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsFileIconProvider(ILogger logger) : IFileIconProvider
{
    private readonly ILogger logger = logger;

    public byte[]? GetIconPng(string path, int size) => null;
}

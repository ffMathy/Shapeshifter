using System.Runtime.Versioning;
using Serilog;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Platform.MacOS;

/// <summary>The frontmost application (and its front window's title, when macOS allows it).</summary>
[SupportedOSPlatform("macos")]
internal sealed class MacClipboardSourceLocator(ILogger logger) : IClipboardSourceLocator
{
    private readonly ILogger logger = logger;

    public ClipboardSource? GetCurrentSource() => null;
}

/// <summary>File and folder icons as Finder shows them.</summary>
[SupportedOSPlatform("macos")]
internal sealed class MacFileIconProvider(ILogger logger) : IFileIconProvider
{
    private readonly ILogger logger = logger;

    public byte[]? GetIconPng(string path, int size) => null;
}

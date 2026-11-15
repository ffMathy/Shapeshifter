using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Platform.Windows;

[SupportedOSPlatform("windows")]
internal static partial class WindowsNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int X;
        public int Y;
    }

    /// <summary>Incremented by Windows on every clipboard change; reading it does not open the clipboard.</summary>
    [LibraryImport("user32.dll")]
    public static partial uint GetClipboardSequenceNumber();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(out Point point);
}

[SupportedOSPlatform("windows")]
internal sealed class WindowsCursorLocator : ICursorLocator
{
    public (int X, int Y)? GetCursorPosition() =>
        WindowsNative.GetCursorPos(out var point) ? (point.X, point.Y) : null;
}

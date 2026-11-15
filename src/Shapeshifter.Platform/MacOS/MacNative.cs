using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Platform.MacOS;

[SupportedOSPlatform("macos")]
internal static partial class MacNative
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [StructLayout(LayoutKind.Sequential)]
    public struct CGPoint
    {
        public double X;
        public double Y;
    }

    [LibraryImport(ObjC, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr objc_getClass(string name);

    [LibraryImport(ObjC, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr sel_registerName(string name);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    public static partial IntPtr SendIntPtr(IntPtr receiver, IntPtr selector);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    public static partial nint SendNInt(IntPtr receiver, IntPtr selector);

    [LibraryImport(CoreGraphics)]
    public static partial IntPtr CGEventCreate(IntPtr source);

    [LibraryImport(CoreGraphics)]
    public static partial CGPoint CGEventGetLocation(IntPtr cgEvent);

    [LibraryImport(CoreFoundation)]
    public static partial void CFRelease(IntPtr cf);

    /// <summary>Reads <c>[[NSPasteboard generalPasteboard] changeCount]</c>.</summary>
    public static long GetPasteboardChangeCount()
    {
        var pasteboardClass = objc_getClass("NSPasteboard");
        if (pasteboardClass == IntPtr.Zero)
        {
            throw new PlatformNotSupportedException("AppKit is not loaded.");
        }

        var pasteboard = SendIntPtr(pasteboardClass, sel_registerName("generalPasteboard"));
        return SendNInt(pasteboard, sel_registerName("changeCount"));
    }
}

/// <summary>Pointer position in global display coordinates (points, top-left origin).</summary>
[SupportedOSPlatform("macos")]
internal sealed class MacCursorLocator : ICursorLocator
{
    public (int X, int Y)? GetCursorPosition()
    {
        var cgEvent = MacNative.CGEventCreate(IntPtr.Zero);
        if (cgEvent == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var location = MacNative.CGEventGetLocation(cgEvent);
            return ((int)location.X, (int)location.Y);
        }
        finally
        {
            MacNative.CFRelease(cgEvent);
        }
    }
}

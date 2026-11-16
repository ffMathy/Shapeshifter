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
    private const string AppKit = "/System/Library/Frameworks/AppKit.framework/AppKit";

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

    /// <summary>A Core Graphics rectangle (also <c>NSRect</c> on 64-bit): origin and size in doubles.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CGRect
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;
    }

    /// <summary><c>id objc_msgSend(id, SEL, id)</c>: a message taking one object (or pointer) argument.</summary>
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    public static partial IntPtr SendIntPtrIntPtr(IntPtr receiver, IntPtr selector, IntPtr argument);

    /// <summary><c>id objc_msgSend(id, SEL, const char *)</c>, e.g. <c>+[NSString stringWithUTF8String:]</c>.</summary>
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr SendIntPtrUtf8(IntPtr receiver, IntPtr selector, string argument);

    /// <summary><c>int objc_msgSend(id, SEL)</c>, e.g. <c>-[NSRunningApplication processIdentifier]</c> (a <c>pid_t</c>).</summary>
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    public static partial int SendInt32(IntPtr receiver, IntPtr selector);

    /// <summary>
    /// <c>CGImageRef objc_msgSend(id, SEL, NSRect *, NSGraphicsContext *, NSDictionary *)</c>, i.e.
    /// <c>-[NSImage CGImageForProposedRect:context:hints:]</c>. The rectangle is passed by pointer, not by value.
    /// </summary>
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    public static partial IntPtr SendCGImageForProposedRect(IntPtr receiver, IntPtr selector, ref CGRect proposedRect, IntPtr context, IntPtr hints);

    [LibraryImport(CoreGraphics)]
    public static partial IntPtr CGWindowListCopyWindowInfo(uint option, uint relativeToWindow);

    [LibraryImport(CoreGraphics)]
    public static partial IntPtr CGColorSpaceCreateDeviceRGB();

    [LibraryImport(CoreGraphics)]
    public static partial void CGColorSpaceRelease(IntPtr colorSpace);

    [LibraryImport(CoreGraphics)]
    public static partial IntPtr CGBitmapContextCreate(IntPtr data, nuint width, nuint height, nuint bitsPerComponent, nuint bytesPerRow, IntPtr colorSpace, uint bitmapInfo);

    [LibraryImport(CoreGraphics)]
    public static partial void CGContextSetInterpolationQuality(IntPtr context, int quality);

    [LibraryImport(CoreGraphics)]
    public static partial void CGContextDrawImage(IntPtr context, CGRect rect, IntPtr image);

    [LibraryImport(CoreGraphics)]
    public static partial void CGContextRelease(IntPtr context);

    [LibraryImport(CoreGraphics)]
    public static partial nuint CGImageGetWidth(IntPtr image);

    [LibraryImport(CoreGraphics)]
    public static partial nuint CGImageGetHeight(IntPtr image);

    [LibraryImport(CoreFoundation)]
    public static partial nint CFArrayGetCount(IntPtr array);

    [LibraryImport(CoreFoundation)]
    public static partial IntPtr CFArrayGetValueAtIndex(IntPtr array, nint index);

    [LibraryImport(CoreFoundation)]
    public static partial IntPtr CFDictionaryGetValue(IntPtr dictionary, IntPtr key);

    /// <summary><c>Boolean CFNumberGetValue(CFNumberRef, CFNumberType, void *)</c>; <c>Boolean</c> is an unsigned char.</summary>
    [LibraryImport(CoreFoundation)]
    public static partial byte CFNumberGetValue(IntPtr number, nint numberType, out int value);

    [LibraryImport(CoreFoundation)]
    public static partial nuint CFGetTypeID(IntPtr cf);

    [LibraryImport(CoreFoundation)]
    public static partial nuint CFNumberGetTypeID();

    [LibraryImport(CoreFoundation)]
    public static partial nuint CFStringGetTypeID();

    /// <summary>Reads an exported <c>CFStringRef</c> constant (such as <c>kCGWindowOwnerPID</c>) from Core Graphics.</summary>
    public static IntPtr GetCoreGraphicsStringConstant(string name) =>
        NativeLibrary.TryLoad(CoreGraphics, out var library) && NativeLibrary.TryGetExport(library, name, out var address)
            ? Marshal.ReadIntPtr(address)
            : IntPtr.Zero;

    /// <summary>Makes sure AppKit is loaded, so <c>objc_getClass</c> can find classes such as <c>NSWorkspace</c>.</summary>
    public static bool TryLoadAppKit() => AppKitLoaded.Value;

    private static readonly Lazy<bool> AppKitLoaded = new(() => NativeLibrary.TryLoad(AppKit, out _));

    /// <summary>Copies an <c>NSString</c> (or toll-free bridged <c>CFStringRef</c>) into a managed string.</summary>
    /// <remarks>Must be called inside an autorelease pool: <c>UTF8String</c> returns autoreleased storage.</remarks>
    public static string? ToManagedString(IntPtr nsString)
    {
        if (nsString == IntPtr.Zero)
        {
            return null;
        }

        var utf8 = SendIntPtr(nsString, sel_registerName("UTF8String"));
        return utf8 == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(utf8);
    }

    /// <summary>Creates an autoreleased <c>NSString</c>; must be called inside an autorelease pool.</summary>
    public static IntPtr CreateNSString(string value)
    {
        var stringClass = objc_getClass("NSString");
        return stringClass == IntPtr.Zero ? IntPtr.Zero : SendIntPtrUtf8(stringClass, sel_registerName("stringWithUTF8String:"), value);
    }

    [LibraryImport(ObjC)]
    public static partial IntPtr objc_autoreleasePoolPush();

    [LibraryImport(ObjC)]
    public static partial void objc_autoreleasePoolPop(IntPtr pool);

    /// <summary>Reads <c>[[NSPasteboard generalPasteboard] changeCount]</c>.</summary>
    /// <remarks>
    /// This is polled from thread-pool threads, which have no autorelease pool. Without a local pool, whatever
    /// the messages autorelease lands in the thread's implicit pool, which is never drained because
    /// thread-pool threads do not exit, so memory would grow with every poll.
    /// </remarks>
    public static long GetPasteboardChangeCount()
    {
        var pasteboardClass = objc_getClass("NSPasteboard");
        if (pasteboardClass == IntPtr.Zero)
        {
            throw new PlatformNotSupportedException("AppKit is not loaded.");
        }

        var pool = objc_autoreleasePoolPush();
        try
        {
            var pasteboard = SendIntPtr(pasteboardClass, sel_registerName("generalPasteboard"));
            return SendNInt(pasteboard, sel_registerName("changeCount"));
        }
        finally
        {
            objc_autoreleasePoolPop(pool);
        }
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

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Shapeshifter.Platform.MacOS;

/// <summary>
/// Puts the system's frosted material behind a borderless window's content, rounded to its sheet, with the system
/// shadow following the rounded shape.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia's own AcrylicBlur cannot be used for this (Avalonia 12.1, Avalonia.Native): it keeps the window background
/// an opaque grey, its NSVisualEffectView fills the whole window, and that view follows the window's key state, so a
/// window that is never key (the picker) shows the flat inactive material. Instead the window is left Transparent (a
/// clear background) and a view of our own is inserted into its content view, as a sibling below Avalonia's rendering
/// view, filling the window: an NSVisualEffectView forced active, or on macOS 26 optionally an NSGlassEffectView (Liquid
/// Glass). Avalonia never touches views it did not add. Avalonia turns the shadow off for borderless windows, so it is
/// turned back on; macOS derives it from the window's alpha, which is why it must be recomputed after each new frame.
/// </para>
/// <para>See <c>.github/workflows/live-screenshots.yml</c>, which captures this on a real Mac.</para>
/// </remarks>
[SupportedOSPlatform("macos")]
public static partial class MacWindowMaterial
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";

    // AppKit constants.
    private const nint BlendingModeBehindWindow = 0;
    private const nint StateActive = 1;
    private const nint MaterialPopover = 6;
    private const nint WindowBelow = -1;
    private const nuint WidthAndHeightSizable = 2 | 16;

    /// <summary>The kind of material to put behind the content.</summary>
    public enum Kind
    {
        /// <summary>NSVisualEffectView, the popover material, behind the window: frosted, like menus and popovers.</summary>
        Vibrancy,

        /// <summary>NSGlassEffectView (macOS 26 and later; falls back to <see cref="Vibrancy"/>): Liquid Glass.</summary>
        LiquidGlass,
    }

    /// <summary>
    /// Inserts the material behind the content of <paramref name="nsWindow"/>, once; later calls only refresh the
    /// shadow. Returns false (and changes nothing) when a class or selector it needs is missing.
    /// </summary>
    /// <param name="nsWindow">The NSWindow.</param>
    /// <param name="renderView">Avalonia's rendering NSView, which stays above the material.</param>
    /// <param name="cornerRadius">The radius of the sheet, in points.</param>
    /// <param name="width">The window's width in points.</param>
    /// <param name="height">The window's height in points.</param>
    /// <param name="kind">The material.</param>
    /// <param name="applied">The material that was used.</param>
    public static bool Attach(IntPtr nsWindow, IntPtr renderView, double cornerRadius, double width, double height, Kind kind, out Kind applied)
    {
        applied = kind;
        if (nsWindow == IntPtr.Zero || renderView == IntPtr.Zero)
        {
            return false;
        }

        var contentView = Send(nsWindow, Sel("contentView"));
        if (contentView == IntPtr.Zero || SendBoolArgPtr(renderView, Sel("isDescendantOf:"), contentView) == 0)
        {
            return false;
        }

        // Already attached to this window: only bring the shadow back.
        if (Attached.Contains(nsWindow))
        {
            RefreshShadow(nsWindow);
            return true;
        }

        var glassClass = objc_getClass("NSGlassEffectView");
        if (kind == Kind.LiquidGlass && glassClass == IntPtr.Zero)
        {
            applied = kind = Kind.Vibrancy;
        }

        var viewClass = kind == Kind.LiquidGlass ? glassClass : objc_getClass("NSVisualEffectView");
        if (viewClass == IntPtr.Zero)
        {
            return false;
        }

        var view = Send(Send(viewClass, Sel("alloc")), Sel("init"));
        if (view == IntPtr.Zero)
        {
            return false;
        }

        SendRect(view, Sel("setFrame:"), new CGRect { Width = width, Height = height });
        SendNUInt(view, Sel("setAutoresizingMask:"), WidthAndHeightSizable);
        if (kind == Kind.LiquidGlass)
        {
            SendDouble(view, Sel("setCornerRadius:"), cornerRadius);
        }
        else
        {
            SendNInt(view, Sel("setBlendingMode:"), BlendingModeBehindWindow);
            SendNInt(view, Sel("setMaterial:"), MaterialPopover);

            // Active even though the window is never key: otherwise the material is drawn flat and grey.
            SendNInt(view, Sel("setState:"), StateActive);
            SendBool(view, Sel("setWantsLayer:"), 1);
            var layer = Send(view, Sel("layer"));
            if (layer != IntPtr.Zero)
            {
                SendDouble(layer, Sel("setCornerRadius:"), cornerRadius);
                SendBool(layer, Sel("setMasksToBounds:"), 1);
                if (RespondsTo(layer, "setCornerCurve:"))
                {
                    // kCACornerCurveContinuous: the squircle corners of macOS windows and sheets.
                    SendPtr(layer, Sel("setCornerCurve:"), NSString("continuous"));
                }
            }
        }

        Attached.Add(nsWindow);
        SendAddSubview(contentView, Sel("addSubview:positioned:relativeTo:"), view, WindowBelow, renderView);
        Send(view, Sel("release"));
        RefreshShadow(nsWindow);
        return true;
    }

    /// <summary>Whether <see cref="Attach"/> already put a material into this window.</summary>
    public static bool IsAttached(IntPtr nsWindow) => Attached.Contains(nsWindow);

    /// <summary>
    /// Turns the system shadow on and recomputes it from the window's current content: call after each frame that
    /// changed the window's shape or size has been drawn.
    /// </summary>
    public static void RefreshShadow(IntPtr nsWindow)
    {
        if (nsWindow == IntPtr.Zero)
        {
            return;
        }

        SendBool(nsWindow, Sel("setHasShadow:"), 1);
        Send(nsWindow, Sel("invalidateShadow"));
    }

    /// <summary>Forgets a window that was closed, so its handle can be reused.</summary>
    public static void Forget(IntPtr nsWindow) => Attached.Remove(nsWindow);

    private static readonly HashSet<IntPtr> Attached = [];

    private static IntPtr Sel(string name) => sel_registerName(name);

    private static bool RespondsTo(IntPtr receiver, string selector) =>
        SendBoolArgPtr(receiver, Sel("respondsToSelector:"), Sel(selector)) != 0;

    private static IntPtr NSString(string value) =>
        SendPtrArgString(objc_getClass("NSString"), Sel("stringWithUTF8String:"), value);

    [StructLayout(LayoutKind.Sequential)]
    private struct CGRect
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;
    }

    [LibraryImport(ObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr objc_getClass(string name);

    [LibraryImport(ObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr sel_registerName(string name);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial IntPtr Send(IntPtr receiver, IntPtr selector);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial void SendPtr(IntPtr receiver, IntPtr selector, IntPtr argument);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr SendPtrArgString(IntPtr receiver, IntPtr selector, string argument);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial void SendNInt(IntPtr receiver, IntPtr selector, nint argument);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial void SendNUInt(IntPtr receiver, IntPtr selector, nuint argument);

    /// <summary>A BOOL argument (a signed char on x64, a bool on arm64: one byte either way).</summary>
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial void SendBool(IntPtr receiver, IntPtr selector, byte argument);

    /// <summary>A BOOL result of a message with one object argument.</summary>
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial byte SendBoolArgPtr(IntPtr receiver, IntPtr selector, IntPtr argument);

    /// <summary>A CGFloat argument (a double on 64-bit macOS).</summary>
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial void SendDouble(IntPtr receiver, IntPtr selector, double argument);

    /// <summary>An NSRect argument (passed by value; no _stret variant is needed, since nothing is returned).</summary>
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial void SendRect(IntPtr receiver, IntPtr selector, CGRect argument);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial void SendAddSubview(IntPtr receiver, IntPtr selector, IntPtr view, nint place, IntPtr relativeTo);
}

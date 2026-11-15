using System.Runtime.InteropServices;

namespace Shapeshifter.Platform.Linux;

/// <summary>Minimal libX11 / libXfixes bindings.</summary>
internal static unsafe partial class X11Native
{
    private const string LibX11 = "libX11.so.6";
    private const string LibXfixes = "libXfixes.so.3";

    public const uint ShiftMask = 1 << 0;
    public const uint LockMask = 1 << 1;
    public const uint ControlMask = 1 << 2;
    public const uint Mod2Mask = 1 << 4; // NumLock on virtually every layout
    public const int GrabModeAsync = 1;
    public const int KeyPress = 2;
    public const int KeyRelease = 3;
    public const int XFixesSelectionNotify = 0;
    public const ulong XFixesSetSelectionOwnerNotifyMask = 1;

    public const ulong XK_v = 0x0076;
    public const ulong XK_Escape = 0xff1b;
    public const ulong XK_Left = 0xff51;
    public const ulong XK_Up = 0xff52;
    public const ulong XK_Right = 0xff53;
    public const ulong XK_Down = 0xff54;
    public const ulong XK_Delete = 0xffff;

    /// <summary>Size of the XEvent union on 64-bit platforms is 192 bytes; allocate generously.</summary>
    public const int XEventSize = 256;

    [StructLayout(LayoutKind.Sequential)]
    public struct XErrorEvent
    {
        public int Type;
        public IntPtr Display;
        public IntPtr ResourceId;
        public IntPtr Serial;
        public byte ErrorCode;
        public byte RequestCode;
        public byte MinorCode;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int XErrorHandler(IntPtr display, XErrorEvent* error);

    [LibraryImport(LibX11)]
    public static partial int XInitThreads();

    [LibraryImport(LibX11)]
    public static partial IntPtr XOpenDisplay(IntPtr displayName);

    [LibraryImport(LibX11)]
    public static partial void XCloseDisplay(IntPtr display);

    [LibraryImport(LibX11)]
    public static partial IntPtr XDefaultRootWindow(IntPtr display);

    [LibraryImport(LibX11)]
    public static partial byte XKeysymToKeycode(IntPtr display, ulong keysym);

    [LibraryImport(LibX11)]
    public static partial void XGrabKey(IntPtr display, int keycode, uint modifiers, IntPtr grabWindow, [MarshalAs(UnmanagedType.Bool)] bool ownerEvents, int pointerMode, int keyboardMode);

    [LibraryImport(LibX11)]
    public static partial void XUngrabKey(IntPtr display, int keycode, uint modifiers, IntPtr grabWindow);

    [LibraryImport(LibX11)]
    public static partial void XSync(IntPtr display, [MarshalAs(UnmanagedType.Bool)] bool discard);

    [LibraryImport(LibX11)]
    public static partial void XFlush(IntPtr display);

    [LibraryImport(LibX11)]
    public static partial int XPending(IntPtr display);

    [LibraryImport(LibX11)]
    public static partial void XNextEvent(IntPtr display, IntPtr eventReturn);

    [LibraryImport(LibX11)]
    public static partial int XConnectionNumber(IntPtr display);

    [LibraryImport(LibX11, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr XInternAtom(IntPtr display, string atomName, [MarshalAs(UnmanagedType.Bool)] bool onlyIfExists);

    [LibraryImport(LibX11)]
    public static partial IntPtr XSetErrorHandler(IntPtr handler);

    [LibraryImport(LibX11)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool XQueryPointer(IntPtr display, IntPtr window, out IntPtr rootReturn, out IntPtr childReturn, out int rootX, out int rootY, out int winX, out int winY, out uint mask);

    [LibraryImport(LibXfixes)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool XFixesQueryExtension(IntPtr display, out int eventBase, out int errorBase);

    [LibraryImport(LibXfixes)]
    public static partial void XFixesSelectSelectionInput(IntPtr display, IntPtr window, IntPtr selection, ulong eventMask);

    [StructLayout(LayoutKind.Sequential)]
    public struct PollFd
    {
        public int Fd;
        public short Events;
        public short Revents;
    }

    public const short PollIn = 0x001;

    [LibraryImport("libc", EntryPoint = "poll", SetLastError = true)]
    public static partial int Poll(PollFd* fds, nuint count, int timeoutMilliseconds);
}

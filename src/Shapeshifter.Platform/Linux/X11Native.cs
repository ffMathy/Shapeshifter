using System.Runtime.InteropServices;

namespace Shapeshifter.Platform.Linux;

/// <summary>Minimal libX11 / libXfixes / libxcb bindings.</summary>
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
    public const int MappingNotify = 34;
    public static readonly IntPtr CurrentTime = IntPtr.Zero;
    public const int XFixesSelectionNotify = 0;
    public const ulong XFixesSetSelectionOwnerNotifyMask = 1;

    public const ulong XK_v = 0x0076;
    public const ulong XK_s = 0x0073;
    public const ulong XK_Escape = 0xff1b;
    public const ulong XK_Left = 0xff51;
    public const ulong XK_Up = 0xff52;
    public const ulong XK_Right = 0xff53;
    public const ulong XK_Down = 0xff54;
    public const ulong XK_Delete = 0xffff;

    /// <summary>XK_1 to XK_9 are their ASCII codes.</summary>
    public const ulong XK_1 = 0x0031;

    /// <summary>XK_KP_1 to XK_KP_9 follow each other.</summary>
    public const ulong XK_KP_1 = 0xffb1;

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

    /// <summary>Ends an active keyboard grab held by this client (for example one activated by a passive key grab).</summary>
    [LibraryImport(LibX11)]
    public static partial void XUngrabKeyboard(IntPtr display, IntPtr time);

    /// <summary>Updates Xlib's cached keyboard mapping after a MappingNotify event.</summary>
    [LibraryImport(LibX11)]
    public static partial void XRefreshKeyboardMapping(IntPtr mappingEvent);

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

    // ---- Moving the keyboard focus between the picker and the window the user works in ----

    public const int ClientMessage = 33;
    public const int RevertToParent = 2;
    public static readonly IntPtr XA_WINDOW = 33;
    public const long SubstructureNotifyMask = 1L << 19;
    public const long SubstructureRedirectMask = 1L << 20;

    [LibraryImport(LibX11)]
    public static partial int XGetWindowProperty(
        IntPtr display, IntPtr window, IntPtr property, long offset, long length, [MarshalAs(UnmanagedType.Bool)] bool delete, IntPtr requestedType,
        out IntPtr actualType, out int actualFormat, out ulong itemCount, out ulong bytesAfter, out IntPtr data);

    [LibraryImport(LibX11)]
    public static partial void XFree(IntPtr data);

    [LibraryImport(LibX11)]
    public static partial int XGetInputFocus(IntPtr display, out IntPtr focus, out int revertTo);

    [LibraryImport(LibX11)]
    public static partial void XSetInputFocus(IntPtr display, IntPtr window, int revertTo, IntPtr time);

    [LibraryImport(LibX11)]
    public static partial void XRaiseWindow(IntPtr display, IntPtr window);

    [LibraryImport(LibX11)]
    public static partial int XSendEvent(IntPtr display, IntPtr window, [MarshalAs(UnmanagedType.Bool)] bool propagate, long eventMask, IntPtr eventSend);

    [LibraryImport(LibXfixes)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool XFixesQueryExtension(IntPtr display, out int eventBase, out int errorBase);

    [LibraryImport(LibXfixes)]
    public static partial void XFixesSelectSelectionInput(IntPtr display, IntPtr window, IntPtr selection, ulong eventMask);

    // libxcb: used where errors must come back per request (for example BadWindow for a window that just closed)
    // instead of through Xlib's process-wide error handler. XCB connections are independent of Xlib's and thread-safe.
    private const string LibXcb = "libxcb.so.1";

    public const uint XcbAtomNone = 0;
    public const uint XcbAtomAny = 0;
    public const uint XcbAtomCardinal = 6;
    public const uint XcbAtomString = 31;
    public const uint XcbAtomWindow = 33;
    public const uint XcbAtomWmName = 39;
    public const uint XcbAtomWmClass = 67;

    /// <summary>xcb_*_cookie_t: the sequence number of a request whose reply is still to be read.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XcbCookie
    {
        public uint Sequence;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XcbScreenIterator
    {
        /// <summary>xcb_screen_t*; its first field is the root window.</summary>
        public IntPtr Data;
        public int Rem;
        public int Index;
    }

    /// <summary>The fixed part of xcb_get_property_reply_t (32 bytes); the value follows it.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XcbGetPropertyReply
    {
        public byte ResponseType;
        public byte Format;
        public ushort Sequence;
        public uint Length;
        public uint Type;
        public uint BytesAfter;
        public uint ValueLength;
    }

    [LibraryImport(LibXcb, EntryPoint = "xcb_connect")]
    public static partial IntPtr XcbConnect(IntPtr displayName, out int screen);

    [LibraryImport(LibXcb, EntryPoint = "xcb_connection_has_error")]
    public static partial int XcbConnectionHasError(IntPtr connection);

    [LibraryImport(LibXcb, EntryPoint = "xcb_disconnect")]
    public static partial void XcbDisconnect(IntPtr connection);

    [LibraryImport(LibXcb, EntryPoint = "xcb_get_setup")]
    public static partial IntPtr XcbGetSetup(IntPtr connection);

    [LibraryImport(LibXcb, EntryPoint = "xcb_setup_roots_iterator")]
    public static partial XcbScreenIterator XcbSetupRootsIterator(IntPtr setup);

    [LibraryImport(LibXcb, EntryPoint = "xcb_screen_next")]
    public static partial void XcbScreenNext(XcbScreenIterator* iterator);

    [LibraryImport(LibXcb, EntryPoint = "xcb_intern_atom")]
    public static partial XcbCookie XcbInternAtom(IntPtr connection, byte onlyIfExists, ushort nameLength, byte* name);

    /// <summary>Returns a malloc'd xcb_intern_atom_reply_t (the atom is the uint at offset 8), or null with a malloc'd error.</summary>
    [LibraryImport(LibXcb, EntryPoint = "xcb_intern_atom_reply")]
    public static partial IntPtr XcbInternAtomReply(IntPtr connection, XcbCookie cookie, out IntPtr error);

    [LibraryImport(LibXcb, EntryPoint = "xcb_get_property")]
    public static partial XcbCookie XcbGetProperty(IntPtr connection, byte delete, uint window, uint property, uint type, uint longOffset, uint longLength);

    /// <summary>Returns a malloc'd <see cref="XcbGetPropertyReply"/>, or null with a malloc'd error.</summary>
    [LibraryImport(LibXcb, EntryPoint = "xcb_get_property_reply")]
    public static partial IntPtr XcbGetPropertyReplyFor(IntPtr connection, XcbCookie cookie, out IntPtr error);

    [LibraryImport(LibXcb, EntryPoint = "xcb_get_property_value")]
    public static partial IntPtr XcbGetPropertyValue(IntPtr reply);

    [LibraryImport(LibXcb, EntryPoint = "xcb_get_property_value_length")]
    public static partial int XcbGetPropertyValueLength(IntPtr reply);

    /// <summary>Releases replies and errors returned by XCB.</summary>
    [LibraryImport("libc", EntryPoint = "free")]
    public static partial void Free(IntPtr pointer);

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

namespace Shapeshifter.UserInterface.WindowsDesktop.Api
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;

    [ExcludeFromCodeCoverage]
    public static class IconApi
    {
        public const uint SHGFI_ICON = 0x100;

        public const uint SHGFI_LARGEICON = 0x0;

        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;

            public IntPtr iIcon;

            public uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAP
        {
            public int Type;

            public int Width;

            public int Height;

            public int WidthBytes;

            public ushort Planes;

            public ushort BitsPixel;

            public IntPtr Bits;
        }

        [DllImport("gdi32", CharSet = CharSet.Auto)]
        public static extern int GetObject(
            IntPtr hgdiobj,
            int cbBuffer,
            out BITMAP lpvObject
            );

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbSizeFileInfo,
            uint uFlags);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false,
            SetLastError = true)]
        public static extern void SHCreateItemFromParsingName(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out] [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItem ppv);

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        public interface IShellItem
        {
            void BindToHandler(
                IntPtr pbc,
                [MarshalAs(UnmanagedType.LPStruct)] Guid bhid,
                [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
                out IntPtr ppv);

            void GetParent(out IShellItem ppsi);

            void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);

            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        [ComImport, Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IShellItemImageFactory
        {
            void GetImage(SIZE size, SIIGBF flags, ref IntPtr phbm);
        }

        public enum SIGDN: uint
        {
            NORMALDISPLAY = 0,

            PARENTRELATIVEPARSING = 0x80018001u,

            PARENTRELATIVEFORADDRESSBAR = 0x8001c001u,

            DESKTOPABSOLUTEPARSING = 0x80028000u,

            PARENTRELATIVEEDITING = 0x80031001u,

            DESKTOPABSOLUTEEDITING = 0x8004c000u,

            FILESYSPATH = 0x80058000u,

            URL = 0x80068000u
        }

        [Flags]
        public enum SIIGBF
        {
            SIIGBF_RESIZETOFIT = 0,

            SIIGBF_BIGGERSIZEOK = 1,

            SIIGBF_MEMORYONLY = 2,

            SIIGBF_ICONONLY = 4,

            SIIGBF_THUMBNAILONLY = 8,

            SIIGBF_INCACHEONLY = 16
        }

        [ExcludeFromCodeCoverage]
        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public readonly int cx;

            public readonly int cy;

            public SIZE(int cx, int cy)
            {
                this.cx = cx;
                this.cy = cy;
            }
        }
    }
}
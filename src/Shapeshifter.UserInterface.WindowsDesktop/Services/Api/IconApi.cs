using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Api
{
    static class IconApi
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
        };

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct BITMAP
        {
            public Int32 Type;
            public Int32 Width;
            public Int32 Height;
            public Int32 WidthBytes;
            public UInt16 Planes;
            public UInt16 BitsPixel;
            public IntPtr Bits;
        }

        [DllImport("gdi32", CharSet = CharSet.Auto)]
        public extern static int GetObject(
            IntPtr hgdiobj, 
            int cbBuffer,
            out BITMAP lpvObject 
        );

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false, SetLastError = true)]
        public static extern void SHCreateItemFromParsingName(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out] [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItem ppv);

        #region Nested type: IShellItem

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        public interface IShellItem
        {
            void BindToHandler(IntPtr pbc,
                               [MarshalAs(UnmanagedType.LPStruct)] Guid bhid,
                               [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
                               out IntPtr ppv);

            void GetParent(out IShellItem ppsi);

            void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);

            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

            void Compare(IShellItem psi, uint hint, out int piOrder);
        };

        #endregion

        #region Nested type: IShellItemImageFactory

        [ComImport, Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IShellItemImageFactory
        {
            void GetImage(SIZE size, SIIGBF flags, ref IntPtr phbm);
        }

        #endregion

        #region Nested type: SIGDN

        public enum SIGDN : uint
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

        #endregion

        #region Nested type: SIIGBF

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

        #endregion

        #region Nested type: SIZE

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

        #endregion

    }
}

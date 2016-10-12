namespace Shapeshifter.WindowsDesktop.Native
{
    using Interfaces;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;

    [ExcludeFromCodeCoverage]
    public class ImageNativeApi: IImageNativeApi
    {
        public const uint BI_RGB = 0x00;

        [DllImport("gdi32.dll")]
        internal static extern bool DeleteObject(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct BITMAPFILEHEADER
        {
            public static readonly short BM = 0x4d42;

            public short bfType;

            public int bfSize;

            public short bfReserved1;

            public short bfReserved2;

            public int bfOffBits;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public readonly int biSize;

            public readonly int biWidth;

            public readonly int biHeight;

            public readonly short biPlanes;

            public readonly short biBitCount;

            public readonly int biCompression;

            public readonly int biSizeImage;

            public readonly int biXPelsPerMeter;

            public readonly int biYPelsPerMeter;

            public readonly int biClrUsed;

            public readonly int biClrImportant;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct BITMAPV5HEADER
        {
            [FieldOffset(0)]
            public uint bV5Size;

            [FieldOffset(4)]
            public int bV5Width;

            [FieldOffset(8)]
            public int bV5Height;

            [FieldOffset(12)]
            public ushort bV5Planes;

            [FieldOffset(14)]
            public ushort bV5BitCount;

            [FieldOffset(16)]
            public uint bV5Compression;

            [FieldOffset(20)]
            public uint bV5SizeImage;

            [FieldOffset(24)]
            public int bV5XPelsPerMeter;

            [FieldOffset(28)]
            public int bV5YPelsPerMeter;

            [FieldOffset(32)]
            public uint bV5ClrUsed;

            [FieldOffset(36)]
            public uint bV5ClrImportant;

            [FieldOffset(40)]
            public uint bV5RedMask;

            [FieldOffset(44)]
            public uint bV5GreenMask;

            [FieldOffset(48)]
            public uint bV5BlueMask;

            [FieldOffset(52)]
            public uint bV5AlphaMask;

            [FieldOffset(56)]
            public uint bV5CSType;

            [FieldOffset(60)]
            public CIEXYZTRIPLE bV5Endpoints;

            [FieldOffset(96)]
            public uint bV5GammaRed;

            [FieldOffset(100)]
            public uint bV5GammaGreen;

            [FieldOffset(104)]
            public uint bV5GammaBlue;

            [FieldOffset(108)]
            public uint bV5Intent;

            [FieldOffset(112)]
            public uint bV5ProfileData;

            [FieldOffset(116)]
            public uint bV5ProfileSize;

            [FieldOffset(120)]
            public uint bV5Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CIEXYZ
        {
            public uint ciexyzX; //FXPT2DOT30

            public uint ciexyzY; //FXPT2DOT30

            public uint ciexyzZ; //FXPT2DOT30
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CIEXYZTRIPLE
        {
            public CIEXYZ ciexyzRed;

            public CIEXYZ ciexyzGreen;

            public CIEXYZ ciexyzBlue;
        }

        bool IImageNativeApi.DeleteObject(IntPtr hObject)
        {
            return DeleteObject(hObject);
        }
    }
}
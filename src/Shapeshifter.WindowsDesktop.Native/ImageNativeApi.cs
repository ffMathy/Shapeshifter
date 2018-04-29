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

		public enum BitmapCompressionMode : uint
		{
			BI_RGB = 0,
			BI_RLE8 = 1,
			BI_RLE4 = 2,
			BI_BITFIELDS = 3,
			BI_JPEG = 4,
			BI_PNG = 5
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct BITMAPV5HEADER
		{
			public uint bV5Size;
			public int bV5Width;
			public int bV5Height;
			public UInt16 bV5Planes;
			public UInt16 bV5BitCount;
			public uint bV5Compression;
			public uint bV5SizeImage;
			public int bV5XPelsPerMeter;
			public int bV5YPelsPerMeter;
			public UInt16 bV5ClrUsed;
			public UInt16 bV5ClrImportant;
			public UInt16 bV5RedMask;
			public UInt16 bV5GreenMask;
			public UInt16 bV5BlueMask;
			public UInt16 bV5AlphaMask;
			public UInt16 bV5CSType;
			public IntPtr bV5Endpoints;
			public UInt16 bV5GammaRed;
			public UInt16 bV5GammaGreen;
			public UInt16 bV5GammaBlue;
			public UInt16 bV5Intent;
			public UInt16 bV5ProfileData;
			public UInt16 bV5ProfileSize;
			public UInt16 bV5Reserved;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 2)]
		public struct BITMAPFILEHEADER
		{
			public static readonly short BM = 0x4d42; // BM
			public short bfType;
			public int bfSize;
			public short bfReserved1;
			public short bfReserved2;
			public int bfOffBits;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct RGBQUAD
		{
			public byte rgbBlue;
			public byte rgbGreen;
			public byte rgbRed;
			public byte rgbReserved;
		}

		bool IImageNativeApi.DeleteObject(IntPtr hObject)
        {
            return DeleteObject(hObject);
        }
		
		public uint GetImageSizeFromBitmapHeader(BITMAPV5HEADER bmi)
		{
			var imageSize = bmi.bV5SizeImage;
			if (bmi.bV5Compression == 0)
				imageSize = (uint)(bmi.bV5Height * bmi.bV5Width * (bmi.bV5BitCount / 8));

			return imageSize;
		}
	}
}
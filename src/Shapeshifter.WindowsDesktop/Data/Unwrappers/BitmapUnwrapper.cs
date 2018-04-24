namespace Shapeshifter.WindowsDesktop.Data.Unwrappers
{
	using System;
	using System.Drawing;
	using System.IO;
	using System.Runtime.InteropServices;
	using System.Windows.Forms;
	using System.Windows.Interop;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using Controls.Window.Interfaces;

	using Interfaces;

	using Native;
	using Native.Interfaces;

	using Services.Images.Interfaces;
	using Shapeshifter.WindowsDesktop.Helpers;
	using static Shapeshifter.WindowsDesktop.Native.ImageNativeApi;

	class BitmapUnwrapper : IBitmapUnwrapper
	{
		readonly IImagePersistenceService imagePersistenceService;
		readonly IClipboardNativeApi clipboardNativeApi;
		readonly IImageNativeApi imageNativeApi;
		readonly IGeneralNativeApi generalNativeApi;
		readonly IMainWindowHandleContainer mainWindowHandleContainer;
		readonly IMemoryUnwrapper memoryUnwrapper;

		public BitmapUnwrapper(
			IImagePersistenceService imagePersistenceService,
			IClipboardNativeApi clipboardNativeApi,
			IImageNativeApi imageNativeApi,
			IGeneralNativeApi generalNativeApi,
			IMainWindowHandleContainer mainWindowHandleContainer,
			IMemoryUnwrapper memoryUnwrapper)
		{
			this.imagePersistenceService = imagePersistenceService;
			this.clipboardNativeApi = clipboardNativeApi;
			this.imageNativeApi = imageNativeApi;
			this.generalNativeApi = generalNativeApi;
			this.mainWindowHandleContainer = mainWindowHandleContainer;
			this.memoryUnwrapper = memoryUnwrapper;
		}

		public bool CanUnwrap(uint format)
		{
			return (format == ClipboardNativeApi.CF_DIBV5) ||
				   (format == ClipboardNativeApi.CF_DIB) ||
				   (format == ClipboardNativeApi.CF_BITMAP) ||
				   (format == ClipboardNativeApi.CF_DIF);
		}

		public byte[] UnwrapStructure(uint format)
		{
			var hBitmap = clipboardNativeApi.GetClipboardData(ClipboardNativeApi.CF_DIBV5);
			var ptr = generalNativeApi.GlobalLock(hBitmap);

			return GetAllBytesFromBitmapHeader(hBitmap, GetBitmapHeader(hBitmap));
		}

		PixelFormat GetPixelFormatFromBitsPerPixel(ushort bitsPerPixel)
		{
			using (CrossThreadLogContext.Add(nameof(bitsPerPixel), bitsPerPixel))
			{
				switch (bitsPerPixel)
				{
					case 2:
						return PixelFormats.BlackWhite;

					case 8:
						return PixelFormats.Gray8;

					case 16:
						return PixelFormats.Gray16;

					case 24:
						return PixelFormats.Bgr24;

					case 32:
						return PixelFormats.Bgra32;

					default:
						throw new InvalidOperationException("Could not recognize the pixel format.");
				}
			}
		}

		BitmapSource DIBV5ToBitmapSource(IntPtr hBitmap)
		{
			var bmi = GetBitmapHeader(hBitmap);
			var allBytes = GetAllBytesFromBitmapHeader(hBitmap, bmi);
			var imageBytes = GetImageBytesFromAllBytes(allBytes, bmi);
			var stride = GetStrideFromBitmapHeader(bmi);
			
			var reversedImageBytes = new byte[imageBytes.Length];
			for (int pBuf = imageBytes.Length, pMap = 0; pBuf > 0; pMap += stride, pBuf -= stride)
				Array.Copy(imageBytes, pMap, reversedImageBytes, pBuf - stride, stride);

			var bmpSource = BitmapSource.Create(
				bmi.bV5Width, bmi.bV5Height,
				bmi.bV5XPelsPerMeter, bmi.bV5YPelsPerMeter,
				GetPixelFormatFromBitsPerPixel(bmi.bV5BitCount), null,
				reversedImageBytes, stride);

			return bmpSource;
		}

		static byte[] GetAllBytesFromBitmapHeader(IntPtr hBitmap, BITMAPV5HEADER bmi)
		{
			var fileHeaderSize = Marshal.SizeOf(typeof(BITMAPFILEHEADER));
			var infoHeaderSize = bmi.bV5Size;
			var fileSize = (int)(fileHeaderSize + infoHeaderSize + bmi.bV5SizeImage);

			var dibBuffer = new byte[fileSize];
			Marshal.Copy(hBitmap, dibBuffer, 0, fileSize);

			var fileHeader = new BITMAPFILEHEADER {
				bfType = BITMAPFILEHEADER.BM,
				bfSize = fileSize,
				bfReserved1 = 0,
				bfReserved2 = 0,
				bfOffBits = (int)(fileHeaderSize + infoHeaderSize + bmi.bV5ClrUsed * Marshal.SizeOf<RGBQUAD>())
			};

			var fileHeaderBytes = BinaryStructHelper.ToByteArray(fileHeader);
			using (var bitmapStream = new MemoryStream())
			{
				bitmapStream.Write(fileHeaderBytes, 0, fileHeaderSize);
				bitmapStream.Write(dibBuffer, 0, dibBuffer.Length);
				bitmapStream.Seek(0, SeekOrigin.Begin);

				return bitmapStream.ToArray();
			}
		}

		static byte[] GetImageBytesFromAllBytes(byte[] bytes, BITMAPV5HEADER bmi)
		{
			var stride = GetStrideFromBitmapHeader(bmi);
			var offset = bmi.bV5Size + Marshal.SizeOf(typeof(BITMAPFILEHEADER)) + bmi.bV5ClrUsed * Marshal.SizeOf<RGBQUAD>();
			if (bmi.bV5Compression == (uint)BitmapCompressionMode.BI_BITFIELDS)
			{
				offset += 12;
			}

			var imageBytes = new byte[bmi.bV5SizeImage];
			Array.Copy(bytes, offset, imageBytes, 0, imageBytes.Length);
		
			return imageBytes;
		}

		static int GetStrideFromBitmapHeader(BITMAPV5HEADER bmi)
		{
			return (int)(bmi.bV5SizeImage / bmi.bV5Height);
		}

		static BITMAPV5HEADER GetBitmapHeader(IntPtr hBitmap)
		{
			return (BITMAPV5HEADER)Marshal.PtrToStructure(hBitmap, typeof(BITMAPV5HEADER));
		}
	}
}
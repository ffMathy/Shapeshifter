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

			return GetAllBytesFromBitmapHeader(hBitmap);
		}

		static byte[] GetAllBytesFromBitmapHeader(IntPtr hBitmap)
		{
			var bmi = (BITMAPV5HEADER)Marshal.PtrToStructure(hBitmap, typeof(BITMAPV5HEADER));

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
	}
}
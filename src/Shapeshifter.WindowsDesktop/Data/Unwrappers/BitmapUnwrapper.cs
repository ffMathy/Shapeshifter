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
	using Shapeshifter.WindowsDesktop.Data.Interfaces;
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

		public bool CanUnwrap(IClipboardFormat format)
		{
			return 
				format.Number == ClipboardNativeApi.CF_DIBV5 ||
				format.Number == ClipboardNativeApi.CF_DIB ||
				format.Number == ClipboardNativeApi.CF_BITMAP;
		}

		public byte[] UnwrapStructure(IClipboardFormat format)
		{
			if(format.Number != ClipboardNativeApi.CF_DIBV5)
				return null;

			var hBitmap = clipboardNativeApi.GetClipboardData(ClipboardNativeApi.CF_DIBV5);

			var ptr = generalNativeApi.GlobalLock(hBitmap);
			try
			{
				return GetAllBytesFromBitmapHeader(ptr);
			}
			finally
			{
				imageNativeApi.DeleteObject(ptr);
			}
		}

		byte[] GetAllBytesFromBitmapHeader(IntPtr hBitmap)
		{
			var bmi = (BITMAPV5HEADER)Marshal.PtrToStructure(hBitmap, typeof(BITMAPV5HEADER));

			var infoHeaderSize = bmi.bV5Size;
			var imageSize = imageNativeApi.GetImageSizeFromBitmapHeader(bmi);
			var fileSize = (int)(infoHeaderSize + imageSize);

			var dibBuffer = new byte[fileSize];
			Marshal.Copy(hBitmap, dibBuffer, 0, fileSize);

			using (var bitmapStream = new MemoryStream())
			{
				bitmapStream.Write(dibBuffer, 0, dibBuffer.Length);
				bitmapStream.Seek(0, SeekOrigin.Begin);

				return bitmapStream.ToArray();
			}
		}
	}
}
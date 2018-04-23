namespace Shapeshifter.WindowsDesktop.Data.Unwrappers
{
	using System;
	using System.Drawing;
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
	using static Shapeshifter.WindowsDesktop.Native.ImageNativeApi;

	class BitmapUnwrapper : IBitmapUnwrapper
	{
		readonly IImagePersistenceService imagePersistenceService;
		readonly IClipboardNativeApi clipboardNativeApi;
		readonly IImageNativeApi imageNativeApi;
		readonly IGeneralNativeApi generalNativeApi;
		readonly IMainWindowHandleContainer mainWindowHandleContainer;

		public BitmapUnwrapper(
			IImagePersistenceService imagePersistenceService,
			IClipboardNativeApi clipboardNativeApi,
			IImageNativeApi imageNativeApi,
			IGeneralNativeApi generalNativeApi,
			IMainWindowHandleContainer mainWindowHandleContainer)
		{
			this.imagePersistenceService = imagePersistenceService;
			this.clipboardNativeApi = clipboardNativeApi;
			this.imageNativeApi = imageNativeApi;
			this.generalNativeApi = generalNativeApi;
			this.mainWindowHandleContainer = mainWindowHandleContainer;
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

			var bmpSrc = DIBV5ToBitmapSource(hBitmap);
			return imagePersistenceService.ConvertBitmapSourceToByteArray(bmpSrc);
		}

		private BitmapSource DIBV5ToBitmapSource(IntPtr hBitmap)
		{
			IntPtr scan0 = IntPtr.Zero;
			var bmi = (BITMAPV5HEADER)Marshal.PtrToStructure(hBitmap, typeof(BITMAPV5HEADER));

			int stride = (int)(bmi.bV5SizeImage / bmi.bV5Height);
			long offset = bmi.bV5Size + bmi.bV5ClrUsed * Marshal.SizeOf<RGBQUAD>();
			if (bmi.bV5Compression == (uint)BitmapCompressionMode.BI_BITFIELDS)
			{
				offset += 12; //bit masks follow the header
			}
			scan0 = new IntPtr(hBitmap.ToInt64() + offset);

			var bmpSource = BitmapSource.Create(
				bmi.bV5Width, bmi.bV5Height,
				bmi.bV5XPelsPerMeter, bmi.bV5YPelsPerMeter,
				PixelFormats.Bgra32, null,
				scan0, (int)bmi.bV5SizeImage, stride);

			return bmpSource;
		}
	}
}
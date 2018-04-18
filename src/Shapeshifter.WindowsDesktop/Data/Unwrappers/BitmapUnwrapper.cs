namespace Shapeshifter.WindowsDesktop.Data.Unwrappers
{
	using System;
	using System.Drawing;
	using System.Windows.Forms;
	using System.Windows.Interop;
	using System.Windows.Media.Imaging;
	using Controls.Window.Interfaces;

	using Interfaces;

	using Native;
	using Native.Interfaces;

	using Services.Images.Interfaces;

	class BitmapUnwrapper : IBitmapUnwrapper
	{
		readonly IImagePersistenceService imagePersistenceService;
		readonly IClipboardNativeApi clipboardNativeApi;
		readonly IImageNativeApi imageNativeApi;
		readonly IMainWindowHandleContainer mainWindowHandleContainer;

		public BitmapUnwrapper(
			IImagePersistenceService imagePersistenceService,
			IClipboardNativeApi clipboardNativeApi,
			IImageNativeApi imageNativeApi,
			IMainWindowHandleContainer mainWindowHandleContainer)
		{
			this.imagePersistenceService = imagePersistenceService;
			this.clipboardNativeApi = clipboardNativeApi;
			this.imageNativeApi = imageNativeApi;
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
			//HACK: we close the clipboard here to avoid it being already open. should definitely be fixed for final release.
			try
			{
				clipboardNativeApi.CloseClipboard();

				//HACK: we are using Windows Forms here to fetch image data. Ugly, but it works.
				var clipboardData = Clipboard.GetDataObject();
				using (var bitmap = (Bitmap)clipboardData.GetData(DataFormats.Bitmap))
				{
					var hBitmap = bitmap.GetHbitmap();
					try
					{
						var image = Imaging.CreateBitmapSourceFromHBitmap(
							hBitmap,
							IntPtr.Zero,
							System.Windows.Int32Rect.Empty,
							BitmapSizeOptions.FromEmptyOptions());

						return imagePersistenceService.ConvertBitmapSourceToByteArray(image);
					}
					finally
					{
						imageNativeApi.DeleteObject(hBitmap);
					}
				}
			}
			finally
			{
				clipboardNativeApi
					.OpenClipboard(mainWindowHandleContainer.Handle);
			}
		}
	}
}
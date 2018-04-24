using System;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using Shapeshifter.WindowsDesktop.Data.Interfaces;
using Shapeshifter.WindowsDesktop.Native;
using Shapeshifter.WindowsDesktop.Services.Images.Interfaces;

namespace Shapeshifter.WindowsDesktop.Data.Wrappers
{
	class BitmapWrapper : IBitmapWrapper
	{
		readonly IImagePersistenceService imagePersistenceService;

		public BitmapWrapper(
			IImagePersistenceService imagePersistenceService)
		{
			this.imagePersistenceService = imagePersistenceService;
		}

		public bool CanWrap(IClipboardData clipboardData)
		{
			return false;
			//var format = clipboardData.RawFormat;
			//return (format == ClipboardNativeApi.CF_DIBV5) ||
			//	   (format == ClipboardNativeApi.CF_DIB) ||
			//	   (format == ClipboardNativeApi.CF_BITMAP) ||
			//	   (format == ClipboardNativeApi.CF_DIF);
		}

		//[StructLayout(LayoutKind.Sequential)]
		//public struct BITMAPINFOHEADER
		//{
		//	public uint biSize;
		//	public int biWidth;
		//	public int biHeight;
		//	public ushort biPlanes;
		//	public ushort biBitCount;
		//	public BitmapCompressionMode biCompression;
		//	public uint biSizeImage;
		//	public int biXPelsPerMeter;
		//	public int biYPelsPerMeter;
		//	public uint biClrUsed;
		//	public uint biClrImportant;

		//	public void Init()
		//	{
		//		biSize = (uint)Marshal.SizeOf(this);
		//	}
		//}

		//[DllImport("gdi32.dll")]
		//static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BITMAPINFO pbmi, uint pila, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

		public IntPtr GetDataPointer(IClipboardData clipboardData)
		{
			var imageData = (IClipboardImageData)clipboardData;
			var source = imagePersistenceService.ConvertByteArrayToBitmapSource(imageData.Image);

			//			DIBSECTION ds;
			//::GetObject(hBitmap, sizeof(DIBSECTION), &ds);
			//			//make sure compression is BI_RGB
			//			ds.dsBmih.biCompression = BI_RGB;
			//			HDC hdc = ::GetDC(NULL);
			//			HBITMAP hbitmap_ddb = ::CreateDIBitmap(
			//				hdc, &ds.dsBmih, CBM_INIT, ds.dsBm.bmBits, (BITMAPINFO*)&ds.dsBmih, DIB_RGB_COLORS);
			//::ReleaseDC(NULL, hdc);

			using (var outStream = new MemoryStream())
			{
				var encoder = new BmpBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(source));
				encoder.Save(outStream);

				return new Bitmap(outStream).GetHbitmap();
			}
		}
	}
}

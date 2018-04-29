namespace Shapeshifter.WindowsDesktop.Native.Interfaces
{
    using System;

	public interface IImageNativeApi
    {
        bool DeleteObject(IntPtr hObject);
		uint GetImageSizeFromBitmapHeader(ImageNativeApi.BITMAPV5HEADER bmi);
	}
}
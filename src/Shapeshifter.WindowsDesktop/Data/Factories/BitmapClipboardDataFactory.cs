namespace Shapeshifter.WindowsDesktop.Data.Factories
{
    using System;
	using System.Runtime.InteropServices;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using Data.Interfaces;

    using Interfaces;

    using Native;

	using Serilog.Context;

	using Shapeshifter.WindowsDesktop.Native.Interfaces;
	using Shapeshifter.WindowsDesktop.Services.Images.Interfaces;
	using static Native.ImageNativeApi;

	class BitmapClipboardDataFactory: IBitmapClipboardDataFactory
    {
		readonly IImagePersistenceService imagePersistenceService;
		readonly IImageNativeApi imageNativeApi;

		public BitmapClipboardDataFactory(
			IImagePersistenceService imagePersistenceService,
			IImageNativeApi imageNativeApi)
        {
			this.imagePersistenceService = imagePersistenceService;
			this.imageNativeApi = imageNativeApi;
		}

		BitmapSource DIBV5ToBitmapSource(byte[] allBytes)
		{
			var bmi = BinaryStructHelper.FromByteArray<BITMAPV5HEADER>(allBytes);
			var imageBytes = GetImageBytesFromAllBytes(allBytes, bmi);

			var stride = GetStrideFromBitmapHeader(bmi);
			var reversedImageBytes = ConvertImageBytesFromBottomUpToTopDown(imageBytes, stride);

			var bmpSource = BitmapSource.Create(
				bmi.bV5Width, bmi.bV5Height,
				bmi.bV5XPelsPerMeter, bmi.bV5YPelsPerMeter,
				GetPixelFormatFromBitsPerPixel(bmi.bV5BitCount), null,
				reversedImageBytes, stride);

			return bmpSource;
		}

		static byte[] ConvertImageBytesFromBottomUpToTopDown(byte[] imageBytes, int stride)
		{
			var reversedImageBytes = new byte[imageBytes.Length];
			for (int pBuf = imageBytes.Length, pMap = 0; pBuf > 0; pMap += stride, pBuf -= stride)
				Array.Copy(imageBytes, pMap, reversedImageBytes, pBuf - stride, stride);

			return reversedImageBytes;
		}

		int GetStrideFromBitmapHeader(BITMAPV5HEADER bmi)
		{
			var imageSize = imageNativeApi.GetImageSizeFromBitmapHeader(bmi);
			return (int)(imageSize / bmi.bV5Height);
		}

		byte[] GetImageBytesFromAllBytes(byte[] bytes, BITMAPV5HEADER bmi)
		{
			var stride = GetStrideFromBitmapHeader(bmi);

			var offset = bmi.bV5Size;
			if(bmi.bV5ClrUsed > 0)
				offset += bmi.bV5ClrUsed * (uint)Marshal.SizeOf<RGBQUAD>();

			var imageSize = imageNativeApi.GetImageSizeFromBitmapHeader(bmi);
			var imageBytes = new byte[imageSize];
			Array.Copy(bytes, offset, imageBytes, 0, imageBytes.Length);

			return imageBytes;
		}

		PixelFormat GetPixelFormatFromBitsPerPixel(ushort bitsPerPixel)
		{
			using (LogContext.PushProperty(nameof(bitsPerPixel), bitsPerPixel))
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

		public IClipboardData BuildData(IClipboardFormat format, byte[] rawData)
        {
            if (!CanBuildData(format))
                throw new InvalidOperationException("The given format is not supported.");

			var bitmapSource = DIBV5ToBitmapSource(rawData);
			return new ClipboardImageData()
            {
                RawData = rawData,
                RawFormat = format,
				Image = imagePersistenceService.ConvertBitmapSourceToByteArray(bitmapSource)
            };
        }

        public bool CanBuildData(IClipboardFormat format)
        {
            return format.Number == ClipboardNativeApi.CF_DIBV5;
        }
    }
}
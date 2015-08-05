using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System.Windows.Media.Imaging;
using System;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Linq;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{

    class ImagePersistenceService : IImagePersistenceService
    {
        public ImagePersistenceService()
        {
        }

        private struct ImageMetaInformation
        {
            public int Width { get; set; }

            public int Height { get; set; }

            public double DpiX { get; set; }

            public double DpiY { get; set; }

            public PixelFormat PixelFormat { get; set; }
        }

        private ImageMetaInformation ConvertByteArrayToMetaInformation(byte[] data)
        {
            var size = Marshal.SizeOf(typeof(ImageMetaInformation));

            var pointer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(data, 0, pointer, size);

                return (ImageMetaInformation)Marshal.PtrToStructure(pointer, typeof(ImageMetaInformation));
            }
            finally
            {
                Marshal.FreeHGlobal(pointer);
            }
        }

        private byte[] ConvertMetaInformationToByteArray(ImageMetaInformation metaInformation)
        {
            var size = Marshal.SizeOf(metaInformation);
            var buffer = new byte[size];

            var pointer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(metaInformation, pointer, true);
            try
            {
                Marshal.Copy(pointer, buffer, 0, size);
                return buffer;
            }
            finally
            {
                Marshal.FreeHGlobal(pointer);
            }
        }

        public byte[] ConvertBitmapSourceToByteArray(BitmapSource bitmap)
        {
            if (bitmap == null) return null;

            var metaInformation = new ImageMetaInformation()
            {
                DpiX = bitmap.DpiX,
                DpiY = bitmap.DpiY,
                Width = bitmap.PixelWidth,
                Height = bitmap.PixelHeight,
                PixelFormat = bitmap.Format
            };

            var metaData = ConvertMetaInformationToByteArray(metaInformation);
            var imageData = ConvertImageDataToByteArray(bitmap);

            return metaData
                .Concat(imageData)
                .ToArray();
        }

        private static byte[] ConvertImageDataToByteArray(BitmapSource bitmap)
        {
            var stride = bitmap.PixelWidth * ((bitmap.Format.BitsPerPixel + 7) / 8);

            var imageData = new byte[bitmap.PixelHeight * stride];
            bitmap.CopyPixels(imageData, stride, 0);
            return imageData;
        }

        public BitmapSource ConvertByteArrayToBitmapSource(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;

            var lengthOfMetaInformation = Marshal.SizeOf(typeof(ImageMetaInformation));
            var metaInformationData = bytes.Take(lengthOfMetaInformation).ToArray();

            var metaInformation = ConvertByteArrayToMetaInformation(metaInformationData);
            if (metaInformation.Width > 0 && metaInformation.Height > 0)
            {
                var imageData = bytes.Skip(lengthOfMetaInformation).ToArray();

                var bytesPerPixel = (metaInformation.PixelFormat.BitsPerPixel + 7) / 8;
                var stride = bytesPerPixel * metaInformation.Width;

                return BitmapSource.Create(metaInformation.Width, metaInformation.Height, metaInformation.DpiX, metaInformation.DpiY, metaInformation.PixelFormat, null, imageData, stride);
            } else
            {
                throw new InvalidOperationException("Tried to load an image without metadata in it.");
            }
        }
    }
}

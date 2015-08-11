using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System.Windows.Media.Imaging;
using System;
using System.Runtime.InteropServices;
using System.Linq;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Images;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{

    class ImagePersistenceService : IImagePersistenceService
    {
        public ImagePersistenceService()
        {
        }

        private static ImageMetaInformation ConvertByteArrayToMetaInformation(byte[] data)
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

            var imageData = ConvertImageDataToByteArray(bitmap);
            return DecorateSourceWithMetaInformation(imageData, metaInformation);
        }

        public byte[] DecorateSourceWithMetaInformation(byte[] source, ImageMetaInformation information)
        {
            var metaData = ConvertMetaInformationToByteArray(information);
            return metaData
                .Concat(source)
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

            if (!DoesSourceHaveMetaInformation(bytes))
            {
                throw new InvalidOperationException("Tried to load an image without metadata in it.");
            }

            return GenerateBitmapSourceFromSource(bytes);
        }

        private static bool DoesSourceHaveMetaInformation(byte[] source)
        {
            var metaInformation = GetMetaInformationFromSource(source);
            return metaInformation.Width > 0 && metaInformation.Height > 0;
        }

        private static ImageMetaInformation GetMetaInformationFromSource(byte[] bytes)
        {
            var metaInformationData = ExtractMetaInformationFromSource(bytes);
            var metaInformation = ConvertByteArrayToMetaInformation(metaInformationData);
            return metaInformation;
        }

        private static byte[] ExtractMetaInformationFromSource(byte[] bytes)
        {
            var metaInformationSize = GetImageMetaInformationStructureSize();
            var metaInformationData = bytes.Take(metaInformationSize).ToArray();
            return metaInformationData;
        }

        private static int GetImageMetaInformationStructureSize()
        {
            return Marshal.SizeOf<ImageMetaInformation>();
        }

        private static BitmapSource GenerateBitmapSourceFromSource(byte[] bytes)
        {
            var metaInformation = GetMetaInformationFromSource(bytes);
            var imageData = ExtractImageDataFromSource(bytes);
            return GenerateBitmapSource(metaInformation, imageData);
        }

        private static BitmapSource GenerateBitmapSource(ImageMetaInformation metaInformation, byte[] imageData)
        {
            var bytesPerPixel = (metaInformation.PixelFormat.BitsPerPixel + 7) / 8;
            var stride = bytesPerPixel * metaInformation.Width;

            return BitmapSource.Create(metaInformation.Width, metaInformation.Height, metaInformation.DpiX, metaInformation.DpiY, metaInformation.PixelFormat, null, imageData, stride);
        }

        private static byte[] ExtractImageDataFromSource(byte[] bytes)
        {
            var metaInformationSize = GetImageMetaInformationStructureSize();
            return bytes.Skip(metaInformationSize).ToArray();
        }
    }
}

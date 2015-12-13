namespace Shapeshifter.WindowsDesktop.Services.Images
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows.Media.Imaging;

    using Api;

    using Interfaces;

    
    class ImagePersistenceService: IImagePersistenceService
    {
        static ImageMetaInformation ConvertByteArrayToMetaInformation(byte[] data)
        {
            return GeneralApi.ByteArrayToStructure<ImageMetaInformation>(data);
        }

        static IEnumerable<byte> ConvertMetaInformationToByteArray(
            ImageMetaInformation metaInformation)
        {
            return GeneralApi.StructureToByteArray(metaInformation);
        }

        public byte[] ConvertBitmapSourceToByteArray(BitmapSource bitmap)
        {
            if (bitmap == null)
            {
                return null;
            }

            var metaInformation = new ImageMetaInformation
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

        public byte[] DecorateSourceWithMetaInformation(
            byte[] source,
            ImageMetaInformation information)
        {
            var metaData = ConvertMetaInformationToByteArray(information);
            return metaData
                .Concat(source)
                .ToArray();
        }

        static byte[] ConvertImageDataToByteArray(BitmapSource bitmap)
        {
            var stride = bitmap.PixelWidth*((bitmap.Format.BitsPerPixel + 7)/8);

            var imageData = new byte[bitmap.PixelHeight*stride];
            bitmap.CopyPixels(imageData, stride, 0);
            return imageData;
        }

        public BitmapSource ConvertByteArrayToBitmapSource(byte[] bytes)
        {
            if ((bytes == null) || (bytes.Length == 0))
            {
                return null;
            }

            if (!DoesSourceHaveMetaInformation(bytes))
            {
                throw new InvalidOperationException(
                    "Tried to load an image without metadata in it.");
            }

            return GenerateBitmapSourceFromSource(bytes);
        }

        static bool DoesSourceHaveMetaInformation(byte[] source)
        {
            var metaInformation = GetMetaInformationFromSource(source);
            return (metaInformation.Width > 0) && (metaInformation.Height > 0);
        }

        static ImageMetaInformation GetMetaInformationFromSource(byte[] bytes)
        {
            var metaInformationData = ExtractMetaInformationFromSource(bytes);
            var metaInformation = ConvertByteArrayToMetaInformation(metaInformationData);
            return metaInformation;
        }

        static byte[] ExtractMetaInformationFromSource(byte[] bytes)
        {
            var metaInformationSize = GetImageMetaInformationStructureSize();
            var metaInformationData = bytes.Take(metaInformationSize)
                                           .ToArray();
            return metaInformationData;
        }

        static int GetImageMetaInformationStructureSize()
        {
            return Marshal.SizeOf<ImageMetaInformation>();
        }

        static BitmapSource GenerateBitmapSourceFromSource(byte[] bytes)
        {
            var metaInformation = GetMetaInformationFromSource(bytes);
            var imageData = ExtractImageDataFromSource(bytes);
            return GenerateBitmapSource(metaInformation, imageData);
        }

        static BitmapSource GenerateBitmapSource(
            ImageMetaInformation metaInformation,
            byte[] imageData)
        {
            var bytesPerPixel = (metaInformation.PixelFormat.BitsPerPixel + 7)/8;
            var stride = bytesPerPixel*metaInformation.Width;

            return BitmapSource.Create(
                metaInformation.Width,
                metaInformation.Height,
                metaInformation.DpiX,
                metaInformation.DpiY,
                metaInformation.PixelFormat,
                null,
                imageData,
                stride);
        }

        static byte[] ExtractImageDataFromSource(byte[] bytes)
        {
            var metaInformationSize = GetImageMetaInformationStructureSize();
            return bytes.Skip(metaInformationSize)
                        .ToArray();
        }
    }
}
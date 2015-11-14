#region

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Shapeshifter.UserInterface.WindowsDesktop.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Images.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Files
{
    [ExcludeFromCodeCoverage]
    internal class FileIconService : IFileIconService
    {
        private readonly IImagePersistenceService imagePersistenceService;

        public FileIconService(IImagePersistenceService imagePersistenceService)
        {
            this.imagePersistenceService = imagePersistenceService;
        }

        public byte[] GetIcon(string path, bool allowThumbnails, int dimensions = 256)
        {
            var bitmapHandle = GenerateBitmapHandle(path, allowThumbnails, dimensions);
            if (bitmapHandle != IntPtr.Zero)
            {
                return GenerateByteArrayFromBitmapHandle(bitmapHandle);
            }
            throw new InvalidOperationException("Could not fetch an icon from the given path.");
        }

        private byte[] GenerateByteArrayFromBitmapHandle(IntPtr bitmapHandle)
        {
            var bitmap = new IconApi.BITMAP();
            AllocateBitmapSpace(bitmapHandle, ref bitmap);

            try
            {
                FillBitmapBitsIntoHandle(bitmap);

                var bitmapSource = CreateBitmapSourceFromHandle(bitmapHandle, bitmap);
                return imagePersistenceService.ConvertBitmapSourceToByteArray(bitmapSource);
            }
            finally
            {
                IconApi.DeleteObject(bitmapHandle);
            }
        }

        private static void FillBitmapBitsIntoHandle(IconApi.BITMAP bitmap)
        {
            var bytes = new byte[bitmap.WidthBytes*bitmap.Height];
            Marshal.Copy(bitmap.Bits, bytes, 0, bytes.Length);
        }

        private static BitmapSource CreateBitmapSourceFromHandle(IntPtr bitmapHandle, IconApi.BITMAP bitmap)
        {
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmapHandle, IntPtr.Zero,
                new Int32Rect(0, 0, bitmap.Width, bitmap.Height),
                BitmapSizeOptions.FromWidthAndHeight(bitmap.Width, bitmap.Height));
            bitmapSource.Freeze();
            return bitmapSource;
        }

        private static void AllocateBitmapSpace(IntPtr bitmapHandle, ref IconApi.BITMAP bitmap)
        {
            var bufferSize = Marshal.SizeOf(bitmap);
            IconApi.GetObject(bitmapHandle, bufferSize, out bitmap);
        }

        private static IntPtr GenerateBitmapHandle(string filePath, bool allowThumbnails, int dimensions)
        {
            try
            {
                return GetFactoryImage(dimensions, filePath, allowThumbnails);
            }
            catch (COMException ex) when (ex.ErrorCode == -2147175936 && allowThumbnails)
            {
                return GetFactoryImage(dimensions, filePath, false);
            }
        }

        private static IntPtr GetFactoryImage(int dimensions, string path, bool allowThumbnails)
        {
            var factory = GenerateShellItemImageFactory(path);

            var bitmapHandle = IntPtr.Zero;

            var imageScope = GenerateScopeFromThumbnailInformation(allowThumbnails);
            factory.GetImage(new IconApi.SIZE(dimensions, dimensions), imageScope, ref bitmapHandle);

            return bitmapHandle;
        }

        private static IconApi.IShellItemImageFactory GenerateShellItemImageFactory(string path)
        {
            var uniqueId = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");

            IconApi.IShellItem rawFactory;
            IconApi.SHCreateItemFromParsingName(path, IntPtr.Zero, uniqueId, out rawFactory);

            var factory = (IconApi.IShellItemImageFactory) rawFactory;
            return factory;
        }

        private static IconApi.SIIGBF GenerateScopeFromThumbnailInformation(bool allowThumbnails)
        {
            var type = allowThumbnails
                ? IconApi.SIIGBF.SIIGBF_THUMBNAILONLY
                : IconApi.SIIGBF.SIIGBF_ICONONLY;
            return type | IconApi.SIIGBF.SIIGBF_BIGGERSIZEOK;
        }
    }
}
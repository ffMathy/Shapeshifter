namespace Shapeshifter.WindowsDesktop.Services.Files
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media.Imaging;

    using Images.Interfaces;

    using Interfaces;

    using Native;
    using Native.Interfaces;

    class FileIconService: IFileIconService
    {
        readonly IImagePersistenceService imagePersistenceService;

        readonly IIconNativeApi iconNativeApi;
		readonly IImageNativeApi imageNativeApi;

        public FileIconService(
            IImagePersistenceService imagePersistenceService,
            IIconNativeApi iconNativeApi,
			IImageNativeApi imageNativeApi)
        {
            this.imagePersistenceService = imagePersistenceService;
            this.iconNativeApi = iconNativeApi;
			this.imageNativeApi = imageNativeApi;
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

        byte[] GenerateByteArrayFromBitmapHandle(IntPtr bitmapHandle)
        {
            var bitmap = new IconNativeApi.BITMAP();
            AllocateBitmapSpace(bitmapHandle, ref bitmap);

            try
            {
                FillBitmapBitsIntoHandle(bitmap);

                var bitmapSource = CreateBitmapSourceFromHandle(bitmapHandle, bitmap);
                return imagePersistenceService.ConvertBitmapSourceToByteArray(bitmapSource);
            }
            finally
            {
                imageNativeApi.DeleteObject(bitmapHandle);
            }
        }

        static void FillBitmapBitsIntoHandle(IconNativeApi.BITMAP bitmap)
        {
            var bytes = new byte[bitmap.WidthBytes*bitmap.Height];
            Marshal.Copy(bitmap.Bits, bytes, 0, bytes.Length);
        }

        static BitmapSource CreateBitmapSourceFromHandle(IntPtr bitmapHandle, IconNativeApi.BITMAP bitmap)
        {
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                bitmapHandle,
                IntPtr.Zero,
                new Int32Rect(
                    0,
                    0,
                    bitmap.Width,
                    bitmap.Height),
                BitmapSizeOptions
                    .FromWidthAndHeight(
                        bitmap
                            .Width,
                        bitmap
                            .Height));
            bitmapSource.Freeze();
            return bitmapSource;
        }

        void AllocateBitmapSpace(IntPtr bitmapHandle, ref IconNativeApi.BITMAP bitmap)
        {
            var bufferSize = Marshal.SizeOf(bitmap);
            iconNativeApi.GetObject(bitmapHandle, bufferSize, out bitmap);
        }

        IntPtr GenerateBitmapHandle(string filePath, bool allowThumbnails, int dimensions)
        {
            try
            {
                return GetFactoryImage(dimensions, filePath, allowThumbnails);
            }
            catch (COMException ex) when ((ex.ErrorCode == -2147175936) && allowThumbnails)
            {
                return GetFactoryImage(dimensions, filePath, false);
            }
        }

        IntPtr GetFactoryImage(int dimensions, string path, bool allowThumbnails)
        {
            var factory = GenerateShellItemImageFactory(path);

            var bitmapHandle = IntPtr.Zero;

            var imageScope = GenerateScopeFromThumbnailInformation(allowThumbnails);
            factory.GetImage(new IconNativeApi.SIZE(dimensions, dimensions), imageScope, ref bitmapHandle);

            return bitmapHandle;
        }

        IconNativeApi.IShellItemImageFactory GenerateShellItemImageFactory(string path)
        {
            var uniqueId = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");

            IconNativeApi.IShellItem rawFactory;
            iconNativeApi.SHCreateItemFromParsingName(path, IntPtr.Zero, uniqueId, out rawFactory);

            // ReSharper disable once SuspiciousTypeConversion.Global
            var factory = (IconNativeApi.IShellItemImageFactory) rawFactory;

            return factory;
        }

        static IconNativeApi.SIIGBF GenerateScopeFromThumbnailInformation(bool allowThumbnails)
        {
            var type = allowThumbnails
                           ? IconNativeApi.SIIGBF.SIIGBF_THUMBNAILONLY
                           : IconNativeApi.SIIGBF.SIIGBF_ICONONLY;
            return type | IconNativeApi.SIIGBF.SIIGBF_BIGGERSIZEOK;
        }
    }
}
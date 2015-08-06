using System;
using System.Runtime.InteropServices;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using static Shapeshifter.UserInterface.WindowsDesktop.Services.Api.IconApi;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    class FileIconService : IFileIconService
    {
        private readonly IImagePersistenceService imagePersistenceService;

        public FileIconService(IImagePersistenceService imagePersistenceService)
        {
            this.imagePersistenceService = imagePersistenceService;
        }

        public byte[] GetIcon(string path, bool allowThumbnails, int dimensions = 256)
        {
            var bitmapHandle = IntPtr.Zero;

            var uniqueId = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");

            IShellItem rawFactory;
            SHCreateItemFromParsingName(path, IntPtr.Zero, uniqueId, out rawFactory);

            var factory = (IShellItemImageFactory)rawFactory;

            var iconOnlyScope = SIIGBF.SIIGBF_ICONONLY;
            var sizeScope = SIIGBF.SIIGBF_BIGGERSIZEOK;
            var scope = (allowThumbnails ? SIIGBF.SIIGBF_THUMBNAILONLY : iconOnlyScope) | sizeScope;

            try
            {
                factory.GetImage(new SIZE(dimensions, dimensions), scope, ref bitmapHandle);
            }
            catch (COMException ex) when (ex.ErrorCode == -2147175936 && allowThumbnails)
            {
                //no thumbnail available. fetch the icon instead.
                factory.GetImage(new SIZE(dimensions, dimensions), iconOnlyScope | sizeScope, ref bitmapHandle);
            }

            if (bitmapHandle != IntPtr.Zero)
            {
                var bitmap = new BITMAP();
                var bufferSize = Marshal.SizeOf(bitmap);

                GetObject(bitmapHandle, bufferSize, out bitmap);

                try
                {
                    var bytes = new byte[bitmap.WidthBytes * bitmap.Height];
                    Marshal.Copy(bitmap.Bits, bytes, 0, bytes.Length);

                    var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmapHandle, IntPtr.Zero, new Int32Rect(0, 0, bitmap.Width, bitmap.Height), BitmapSizeOptions.FromWidthAndHeight(bitmap.Width, bitmap.Height));
                    bitmapSource.Freeze();

                    return imagePersistenceService.ConvertBitmapSourceToByteArray(bitmapSource);
                }
                finally
                {
                    DeleteObject(bitmapHandle);
                }
            }
            else
            {

                //the handle is 0, assume that the data comes from Windows itself.
                return null;

            }
        }
    }
}

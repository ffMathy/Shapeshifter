using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Unwrappers.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Shapeshifter.UserInterface.WindowsDesktop.Api.ImageApi;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Unwrappers
{
    class BitmapUnwrapper : IMemoryUnwrapper
    {
        readonly IImagePersistenceService imagePersistenceService;

        public BitmapUnwrapper(
            IImagePersistenceService imagePersistenceService)
        {
            this.imagePersistenceService = imagePersistenceService;
        }

        public bool CanUnwrap(uint format)
        {
            return format == ClipboardApi.CF_DIBV5 ||
                format == ClipboardApi.CF_DIB ||
                format == ClipboardApi.CF_BITMAP ||
                format == ClipboardApi.CF_DIF;
        }

        public byte[] UnwrapStructure(uint format)
        {
            //TODO: it is very sad that we invoke System.Drawing here to get the job done. probably not very optimal.
            
            var pointer = ClipboardApi.GetClipboardData(ClipboardApi.CF_DIBV5);
            var infoHeader =
                (BITMAPV5HEADER)Marshal.PtrToStructure(pointer, typeof(BITMAPV5HEADER));

            using (var drawingBitmap = CreateDrawingBitmapFromPointer(pointer, infoHeader))
            {
                var renderTargetBitmapSource = new RenderTargetBitmap(infoHeader.bV5Width,
                                                          infoHeader.bV5Height,
                                                          96, 96, PixelFormats.Pbgra32);
                var visual = new DrawingVisual();
                var drawingContext = visual.RenderOpen();

                try
                {
                    drawingContext.DrawImage(CreateBitmapSourceFromBitmap(drawingBitmap),
                                             new Rect(0, 0, infoHeader.bV5Width,
                                                      infoHeader.bV5Height));
                }
                finally
                {
                    drawingContext.Close();
                }

                renderTargetBitmapSource.Render(visual);

                return imagePersistenceService.ConvertBitmapSourceToByteArray(renderTargetBitmapSource);
            }
        }

        private static Bitmap CreateDrawingBitmapFromPointer(IntPtr pointer, BITMAPV5HEADER infoHeader)
        {
            return new Bitmap(
                infoHeader.bV5Width, 
                infoHeader.bV5Height,
                (int)(infoHeader.bV5SizeImage / infoHeader.bV5Height),
                DrawingPixelFormat.Format32bppArgb,
                new IntPtr(pointer.ToInt64() + infoHeader.bV5Size));
        }

        private static BitmapSource CreateBitmapSourceFromBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            var bitmapHandle = bitmap.GetHbitmap();

            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    bitmapHandle,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(bitmapHandle);
            }
        }
    }
}

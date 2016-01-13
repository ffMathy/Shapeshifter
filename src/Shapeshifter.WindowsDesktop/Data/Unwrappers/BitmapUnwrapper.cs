namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Unwrappers
{
    using System.Windows;

    using Interfaces;

    using Native;
    using Native.Interfaces;

    using Services.Images.Interfaces;

    using Window.Interfaces;

    class BitmapUnwrapper: IMemoryUnwrapper
    {
        readonly IImagePersistenceService imagePersistenceService;
        readonly IClipboardNativeApi clipboardNativeApi;
        readonly IMainWindowHandleContainer mainWindowHandleContainer;

        public BitmapUnwrapper(
            IImagePersistenceService imagePersistenceService,
            IClipboardNativeApi clipboardNativeApi,
            IMainWindowHandleContainer mainWindowHandleContainer)
        {
            this.imagePersistenceService = imagePersistenceService;
            this.clipboardNativeApi = clipboardNativeApi;
            this.mainWindowHandleContainer = mainWindowHandleContainer;
        }

        public bool CanUnwrap(uint format)
        {
            return (format == ClipboardNativeApi.CF_DIBV5) ||
                   (format == ClipboardNativeApi.CF_DIB) ||
                   (format == ClipboardNativeApi.CF_BITMAP) ||
                   (format == ClipboardNativeApi.CF_DIF);
        }

        public byte[] UnwrapStructure(uint format)
        {
            //HACK: we close the clipboard here to avoid it being already open. should definitely be fixed for final release.
            try
            {
                clipboardNativeApi.CloseClipboard();

                var image = Clipboard.GetImage();
                return imagePersistenceService
                    .ConvertBitmapSourceToByteArray(image);
            }
            finally
            {
                clipboardNativeApi
                    .OpenClipboard(mainWindowHandleContainer.Handle);
            }
        }
    }
}
using WindowsClipboard = System.Windows.Clipboard;

namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
    using System;
    using System.Collections.Specialized;
    using System.Windows.Media.Imaging;

    using Data.Interfaces;

    using Infrastructure.Handles.Factories.Interfaces;
    using Infrastructure.Logging.Interfaces;

    using Interfaces;

    using Messages.Interceptors.Interfaces;

    using Native;
    using Native.Interfaces;

    class ClipboardInjectionService: IClipboardInjectionService
    {
        readonly IClipboardCopyInterceptor clipboardCopyInterceptor;

        readonly IClipboardHandleFactory clipboardHandleFactory;

        readonly IMemoryHandleFactory memoryHandleFactory;

        readonly ILogger logger;

        readonly IClipboardNativeApi clipboardNativeApi;

        readonly IGeneralNativeApi generalNativeApi;

        public ClipboardInjectionService(
            IClipboardCopyInterceptor clipboardCopyInterceptor,
            IClipboardHandleFactory clipboardHandleFactory,
            IMemoryHandleFactory memoryHandleFactory,
            ILogger logger,
            IClipboardNativeApi clipboardNativeApi,
            IGeneralNativeApi generalNativeApi)
        {
            this.clipboardCopyInterceptor = clipboardCopyInterceptor;
            this.clipboardHandleFactory = clipboardHandleFactory;
            this.memoryHandleFactory = memoryHandleFactory;
            this.logger = logger;
            this.clipboardNativeApi = clipboardNativeApi;
            this.generalNativeApi = generalNativeApi;
        }

        public void InjectData(IClipboardDataPackage package)
        {
            clipboardCopyInterceptor.SkipNext();

            using (clipboardHandleFactory.StartNewSession())
            {
                clipboardNativeApi.EmptyClipboard();
                InjectPackageContents(package);
            }

            logger.Information("Clipboard package has been injected to the clipboard.", 1);
        }

        void InjectPackageContents(IClipboardDataPackage package)
        {
            foreach (var clipboardData in package.Contents)
            {
                InjectClipboardData(clipboardData);
            }
        }

        void InjectClipboardData(IClipboardData clipboardData)
        {
            using (var memoryHandle = memoryHandleFactory.AllocateInMemory(clipboardData.RawData))
            {
                var globalPointer = AllocateInMemory(clipboardData);

                var target = generalNativeApi.GlobalLock(globalPointer);
                if (target == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Could not allocate memory.");
                }

                generalNativeApi.CopyMemory(
                    target,
                    memoryHandle.Pointer,
                    (uint) clipboardData.RawData.Length);

                generalNativeApi.GlobalUnlock(target);

                if (clipboardNativeApi.SetClipboardData(clipboardData.RawFormat, globalPointer) !=
                    IntPtr.Zero)
                {
                    return;
                }

                generalNativeApi.GlobalFree(globalPointer);
                throw new Exception("Could not set clipboard data.");
            }
        }

        IntPtr AllocateInMemory(IClipboardData clipboardData)
        {
            return generalNativeApi.GlobalAlloc(
                GeneralNativeApi.GMEM_ZEROINIT | GeneralNativeApi.GMEM_MOVABLE,
                (UIntPtr) clipboardData.RawData.Length);
        }

        public void InjectImage(BitmapSource image)
        {
            clipboardCopyInterceptor.SkipNext();
            WindowsClipboard.SetImage(image);
        }

        public void InjectText(string text)
        {
            clipboardCopyInterceptor.SkipNext();
            WindowsClipboard.SetText(text);
        }

        public void InjectFiles(params string[] files)
        {
            clipboardCopyInterceptor.SkipNext();

            var collection = new StringCollection();
            collection.AddRange(files);

            WindowsClipboard.SetFileDropList(collection);
        }
    }
}
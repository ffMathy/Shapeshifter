using System.Windows.Media.Imaging;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using WindowsClipboard = System.Windows.Clipboard;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Api;
using System;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard
{
    class ClipboardInjectionService : IClipboardInjectionService
    {
        readonly IClipboardCopyInterceptor clipboardCopyInterceptor;
        readonly IClipboardHandleFactory clipboardHandleFactory;
        readonly IMemoryHandleFactory memoryHandleFactory;
        readonly ILogger logger;

        public ClipboardInjectionService(
            IClipboardCopyInterceptor clipboardCopyInterceptor,
            IClipboardHandleFactory clipboardHandleFactory,
            IMemoryHandleFactory memoryHandleFactory,
            ILogger logger)
        {
            this.clipboardCopyInterceptor = clipboardCopyInterceptor;
            this.clipboardHandleFactory = clipboardHandleFactory;
            this.memoryHandleFactory = memoryHandleFactory;
            this.logger = logger;
        }

        public void InjectData(IClipboardDataPackage package)
        {
            clipboardCopyInterceptor.SkipNext();

            using (clipboardHandleFactory.StartNewSession())
            {
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

                var target = GeneralApi.GlobalLock(globalPointer);
                if (target == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Could not allocate memory.");
                }

                try
                {
                    GeneralApi.CopyMemory(target, memoryHandle.Pointer, (uint)clipboardData.RawData.Length);
                }
                finally
                {
                    GeneralApi.GlobalUnlock(target);
                }

                ClipboardApi.SetClipboardData(clipboardData.RawFormat, globalPointer);
            }
        }

        static IntPtr AllocateInMemory(IClipboardData clipboardData)
        {
            return GeneralApi.GlobalAlloc(
                GeneralApi.GMEM_ZEROINIT | GeneralApi.GMEM_MOVABLE, 
                (UIntPtr)clipboardData.RawData.Length);
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
    }
}

using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media.Imaging;
using Shapeshifter.UserInterface.WindowsDesktop.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Interfaces;
using WindowsClipboard = System.Windows.Clipboard;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard
{
    internal class ClipboardInjectionService : IClipboardInjectionService
    {
        private readonly IClipboardCopyInterceptor clipboardCopyInterceptor;
        private readonly IClipboardHandleFactory clipboardHandleFactory;
        private readonly IMemoryHandleFactory memoryHandleFactory;
        private readonly ILogger logger;

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
                ClipboardApi.EmptyClipboard();
                InjectPackageContents(package);
            }

            logger.Information("Clipboard package has been injected to the clipboard.", 1);
        }

        [ExcludeFromCodeCoverage]
        private void InjectPackageContents(IClipboardDataPackage package)
        {
            foreach (var clipboardData in package.Contents)
            {
                InjectClipboardData(clipboardData);
            }
        }

        [ExcludeFromCodeCoverage]
        private void InjectClipboardData(IClipboardData clipboardData)
        {
            using (var memoryHandle = memoryHandleFactory.AllocateInMemory(clipboardData.RawData))
            {
                var globalPointer = AllocateInMemory(clipboardData);

                var target = GeneralApi.GlobalLock(globalPointer);
                if (target == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Could not allocate memory.");
                }

                GeneralApi.CopyMemory(target, memoryHandle.Pointer, (uint) clipboardData.RawData.Length);

                GeneralApi.GlobalUnlock(target);

                if (ClipboardApi.SetClipboardData(clipboardData.RawFormat, globalPointer) != IntPtr.Zero) return;

                GeneralApi.GlobalFree(globalPointer);
                throw new Exception("Could not set clipboard data.");
            }
        }

        [ExcludeFromCodeCoverage]
        private static IntPtr AllocateInMemory(IClipboardData clipboardData)
        {
            return GeneralApi.GlobalAlloc(
                GeneralApi.GMEM_ZEROINIT | GeneralApi.GMEM_MOVABLE,
                (UIntPtr) clipboardData.RawData.Length);
        }
        
        [ExcludeFromCodeCoverage]
        public void InjectImage(BitmapSource image)
        {
            clipboardCopyInterceptor.SkipNext();
            WindowsClipboard.SetImage(image);
        }

        [ExcludeFromCodeCoverage]
        public void InjectText(string text)
        {
            clipboardCopyInterceptor.SkipNext();
            WindowsClipboard.SetText(text);
        }

        [ExcludeFromCodeCoverage]
        public void InjectFiles(params string[] files)
        {
            clipboardCopyInterceptor.SkipNext();

            var collection = new StringCollection();
            collection.AddRange(files);

            WindowsClipboard.SetFileDropList(collection);
        }
    }
}
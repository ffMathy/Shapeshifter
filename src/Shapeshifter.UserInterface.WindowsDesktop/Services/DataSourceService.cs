using Shapeshifter.Core.Factories.Interfaces;
using System;
using System.Text;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    [ExcludeFromCodeCoverage]
    class DataSourceService : IDataSourceService
    {
        private readonly IImagePersistenceService imagePersistenceService;

        public DataSourceService(IImagePersistenceService imagePersistenceService)
        {
            this.imagePersistenceService = imagePersistenceService;
        }

        private static string GetWindowTitle(IntPtr windowHandle)
        {
            const int numberOfCharacters = 256;
            var buffer = new StringBuilder(numberOfCharacters);

            if (WindowApi.GetWindowText(windowHandle, buffer, numberOfCharacters) > 0)
            {
                return buffer.ToString();
            }
            return null;
        }

        private static BitmapSource GetWindowIcon(IntPtr windowHandle)
        {
            var hIcon = default(IntPtr);
            hIcon = WindowApi.SendMessage(windowHandle, WindowApi.WM_GETICON, WindowApi.ICON_BIG, IntPtr.Zero);

            if (hIcon == IntPtr.Zero)
            {
                hIcon = WindowApi.GetClassLongPtr(windowHandle, WindowApi.GCL_HICON);
            }

            if (hIcon == IntPtr.Zero)
            {
                hIcon = WindowApi.LoadIcon(IntPtr.Zero, (IntPtr)0x7F00/*IDI_APPLICATION*/);
            }

            if (hIcon != IntPtr.Zero)
            {
                return Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            } else {
                throw new InvalidOperationException("Could not load window icon.");
            }
        }

        public IDataSource GetDataSource()
        {
            var activeWindowHandle = WindowApi.GetForegroundWindow();

            var windowTitle = GetWindowTitle(activeWindowHandle);
            var windowIcon = GetWindowIcon(activeWindowHandle);

            var iconBytes = imagePersistenceService.ConvertBitmapSourceToByteArray(windowIcon);
            return new DataSource(iconBytes, windowTitle);
        }
    }
}

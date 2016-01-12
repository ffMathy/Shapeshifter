namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
    using System;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media.Imaging;

    using Data;
    using Data.Interfaces;

    using Images.Interfaces;

    using Infrastructure.Dependencies.Interfaces;

    using Interfaces;

    using Native;
    using Native.Interfaces;

    class DataSourceService
        : IDataSourceService,
          ISingleInstance
    {
        readonly IImagePersistenceService imagePersistenceService;

        readonly IWindowNativeApi windowNativeApi;

        public DataSourceService(
            IImagePersistenceService imagePersistenceService,
            IWindowNativeApi windowNativeApi)
        {
            this.imagePersistenceService = imagePersistenceService;
            this.windowNativeApi = windowNativeApi;
        }

        BitmapSource GetWindowIcon(IntPtr windowHandle)
        {
            var hIcon = windowNativeApi.SendMessage(
                windowHandle,
                (int) Message.WM_GETICON,
                WindowNativeApi.ICON_BIG,
                IntPtr.Zero);
            if (hIcon == IntPtr.Zero)
            {
                hIcon = windowNativeApi.GetClassLongPtr(windowHandle, WindowNativeApi.GCL_HICON);
            }

            if (hIcon == IntPtr.Zero)
            {
                hIcon = windowNativeApi.LoadIcon(IntPtr.Zero, WindowNativeApi.IDI_APPLICATION);
            }

            if (hIcon != IntPtr.Zero)
            {
                return Imaging.CreateBitmapSourceFromHIcon(
                    hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            throw new InvalidOperationException("Could not load window icon.");
        }

        public IDataSource GetDataSource()
        {
            var activeWindowHandle = windowNativeApi.GetForegroundWindow();

            var windowTitle = windowNativeApi.GetWindowTitle(activeWindowHandle);
            var windowIcon = GetWindowIcon(activeWindowHandle);

            var iconBytes = imagePersistenceService.ConvertBitmapSourceToByteArray(windowIcon);
            return new DataSource(iconBytes, windowTitle);
        }
    }
}
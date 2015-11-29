namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media.Imaging;

    using Api;

    using Data;
    using Data.Interfaces;

    using Images.Interfaces;

    using Infrastructure.Dependencies.Interfaces;

    using Interfaces;

    [ExcludeFromCodeCoverage]
    class DataSourceService
        : IDataSourceService,
          ISingleInstance
    {
        readonly IImagePersistenceService imagePersistenceService;

        public DataSourceService(IImagePersistenceService imagePersistenceService)
        {
            this.imagePersistenceService = imagePersistenceService;
        }

        static BitmapSource GetWindowIcon(IntPtr windowHandle)
        {
            var hIcon = WindowApi.SendMessage(
                                              windowHandle,
                                              (int) Message.WM_GETICON,
                                              WindowApi.ICON_BIG,
                                              IntPtr.Zero);
            if (hIcon == IntPtr.Zero)
            {
                hIcon = WindowApi.GetClassLongPtr(windowHandle, WindowApi.GCL_HICON);
            }

            if (hIcon == IntPtr.Zero)
            {
                //TODO: define this constant in the api. it's messy in here.
                hIcon = WindowApi.LoadIcon(IntPtr.Zero, (IntPtr) 0x7F00 /*IDI_APPLICATION*/);
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
            var activeWindowHandle = WindowApi.GetForegroundWindow();

            var windowTitle = WindowApi.GetWindowTitle(activeWindowHandle);
            var windowIcon = GetWindowIcon(activeWindowHandle);

            var iconBytes = imagePersistenceService.ConvertBitmapSourceToByteArray(windowIcon);
            return new DataSource(iconBytes, windowTitle);
        }
    }
}
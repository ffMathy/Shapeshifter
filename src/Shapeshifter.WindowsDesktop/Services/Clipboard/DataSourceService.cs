namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
    using Data;
    using Data.Interfaces;
    using Images.Interfaces;
    using Infrastructure.Dependencies.Interfaces;
    using Interfaces;
    using Messages;
    using Native;
    using Native.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media.Imaging;

    class DataSourceService
        : IDataSourceService,
          ISingleInstance
    {
        readonly IImagePersistenceService imagePersistenceService;
        readonly IWindowNativeApi windowNativeApi;

        readonly IDictionary<IntPtr, byte[]> dataSourceIconCacheLarge;
        readonly IDictionary<IntPtr, byte[]> dataSourceIconCacheSmall;

        public DataSourceService(
            IImagePersistenceService imagePersistenceService,
            IWindowNativeApi windowNativeApi)
        {
            this.imagePersistenceService = imagePersistenceService;
            this.windowNativeApi = windowNativeApi;

            dataSourceIconCacheLarge = new Dictionary<IntPtr, byte[]>();
            dataSourceIconCacheSmall = new Dictionary<IntPtr, byte[]>();
        }

        BitmapSource GetWindowIcon(IntPtr windowHandle, bool bigIconSize = true)
        {
            var hIcon = windowNativeApi.SendMessage(
                windowHandle,
                (int)Message.WM_GETICON,
                bigIconSize ? WindowNativeApi.ICON_LARGE : WindowNativeApi.ICON_SMALL,
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
            lock (this)
            {
                byte[] iconBytesBig;
                if (dataSourceIconCacheLarge.ContainsKey(activeWindowHandle))
                {
                    iconBytesBig = dataSourceIconCacheLarge[activeWindowHandle];
                }
                else
                {
                    var windowIconBig = GetWindowIcon(activeWindowHandle);
                    iconBytesBig = imagePersistenceService.ConvertBitmapSourceToByteArray(windowIconBig);
                    dataSourceIconCacheLarge.Add(activeWindowHandle, iconBytesBig);
                }

                byte[] iconBytesSmall;
                if (dataSourceIconCacheSmall.ContainsKey(activeWindowHandle))
                {
                    iconBytesSmall = dataSourceIconCacheSmall[activeWindowHandle];
                }
                else
                {
                    var windowIconSmall = GetWindowIcon(activeWindowHandle, false);
                    iconBytesSmall = imagePersistenceService.ConvertBitmapSourceToByteArray(windowIconSmall);
                    dataSourceIconCacheSmall.Add(activeWindowHandle, iconBytesSmall);
                }

                var dataSource = new DataSource(iconBytesBig, iconBytesSmall, windowTitle);
                return dataSource;
            }
        }
    }
}
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
	using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media.Imaging;

	using Infrastructure.Caching.Interfaces;

	class DataSourceService
        : IDataSourceService,
          ISingleInstance
    {
        readonly IImagePersistenceService imagePersistenceService;
        readonly IWindowNativeApi windowNativeApi;

        readonly IKeyValueCache<IntPtr, byte[]> dataSourceIconCacheLarge;
        readonly IKeyValueCache<IntPtr, byte[]> dataSourceIconCacheSmall;

        public DataSourceService(
            IImagePersistenceService imagePersistenceService,
            IWindowNativeApi windowNativeApi,
			IKeyValueCache<IntPtr, byte[]> dataSourceIconCacheSmall,
			IKeyValueCache<IntPtr, byte[]> dataSourceIconCacheLarge)
        {
            this.imagePersistenceService = imagePersistenceService;
            this.windowNativeApi = windowNativeApi;

            this.dataSourceIconCacheLarge = dataSourceIconCacheLarge;
            this.dataSourceIconCacheSmall = dataSourceIconCacheSmall;
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
                var iconBytesBig = dataSourceIconCacheLarge.Get(activeWindowHandle);
				if (iconBytesBig == null)
                {
                    var windowIconBig = GetWindowIcon(activeWindowHandle);
                    iconBytesBig = imagePersistenceService.ConvertBitmapSourceToByteArray(windowIconBig);

                    dataSourceIconCacheLarge.Set(activeWindowHandle, iconBytesBig);
                }

                var iconBytesSmall = dataSourceIconCacheSmall.Get(activeWindowHandle);
                if (dataSourceIconCacheSmall == null)
                {
                    var windowIconSmall = GetWindowIcon(activeWindowHandle, false);
                    iconBytesSmall = imagePersistenceService.ConvertBitmapSourceToByteArray(windowIconSmall);

                    dataSourceIconCacheSmall.Set(activeWindowHandle, iconBytesSmall);
                }

                var dataSource = new DataSource(iconBytesBig, iconBytesSmall, windowTitle);
                return dataSource;
            }
        }
    }
}
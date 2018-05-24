namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
    using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Data;
    using Data.Interfaces;
    using Images.Interfaces;
    using Infrastructure.Dependencies.Interfaces;
    using Interfaces;

	using System.Windows;
	using System.Windows.Interop;
	using System.Windows.Media.Imaging;

	using Windows.ApplicationModel;
	using Windows.Management.Deployment;

	using Infrastructure.Caching.Interfaces;

	using Messages;

	using Native;
	using Native.Interfaces;

	using Window.Interfaces;

	class DataSourceService
        : IDataSourceService,
          ISingleInstance
    {
        readonly IImagePersistenceService imagePersistenceService;
        readonly IActiveWindowService activeWindowService;
		readonly IWindowNativeApi windowNativeApi;

		readonly IKeyValueCache<IntPtr, byte[]> dataSourceIconCacheLarge;
        readonly IKeyValueCache<IntPtr, byte[]> dataSourceIconCacheSmall;

		readonly PackageManager packageManager;

		public DataSourceService(
            IImagePersistenceService imagePersistenceService,
            IActiveWindowService activeWindowService,
			IWindowNativeApi windowNativeApi,
			IKeyValueCache<IntPtr, byte[]> dataSourceIconCacheSmall,
            IKeyValueCache<IntPtr, byte[]> dataSourceIconCacheLarge)
        {
            this.imagePersistenceService = imagePersistenceService;
            this.activeWindowService = activeWindowService;
			this.windowNativeApi = windowNativeApi;

			this.dataSourceIconCacheLarge = dataSourceIconCacheLarge;
            this.dataSourceIconCacheSmall = dataSourceIconCacheSmall;

			this.packageManager = new PackageManager();
		}

        public IDataSource GetDataSource()
        {
            lock (this)
			{
                var activeWindowHandle = activeWindowService.ActiveWindowHandle;

				var process = activeWindowService.GetProcessFromWindowHandle(activeWindowHandle);
				if (process.Id != 0)
				{
					var package = packageManager.FindPackageForUser(string.Empty, process.MainModule.ModuleName);
					if (package != null)
						return GetDataSourceFromUniversalWindowsApplication(package);
				}

				var processName = process.ProcessName + ".exe";
                var iconBytesBig = dataSourceIconCacheLarge.Get(activeWindowHandle);
                if (iconBytesBig == null)
                {
                    var windowIconBig = GetWindowIcon(activeWindowHandle);
                    iconBytesBig = imagePersistenceService.ConvertBitmapSourceToByteArray(windowIconBig);

                    dataSourceIconCacheLarge.Set(activeWindowHandle, iconBytesBig);
                }

                var iconBytesSmall = dataSourceIconCacheSmall.Get(activeWindowHandle);
                if (iconBytesSmall == null)
                {
                    var windowIconSmall = GetWindowIcon(activeWindowHandle, false);
                    iconBytesSmall = imagePersistenceService.ConvertBitmapSourceToByteArray(windowIconSmall);

                    dataSourceIconCacheSmall.Set(activeWindowHandle, iconBytesSmall);
                }

                var dataSource = new DataSource(
					iconBytesBig, 
					iconBytesSmall,
                    activeWindowService.GetWindowTitleFromWindowHandle(activeWindowHandle), 
					processName);
                return dataSource;
            }
        }

		IDataSource GetDataSourceFromUniversalWindowsApplication(Package package)
		{
			throw new NotImplementedException();
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
    }
}

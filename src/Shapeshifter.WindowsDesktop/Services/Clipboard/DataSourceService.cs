namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Interop;
	using System.Windows.Media.Imaging;

	using Data;
	using Data.Interfaces;

	using Images.Interfaces;

	using Infrastructure.Dependencies.Interfaces;

	using Interfaces;

	using Messages;

	using Native;
	using Native.Interfaces;

	class DataSourceService
		: IDataSourceService,
		  ISingleInstance
	{
		readonly IImagePersistenceService imagePersistenceService;
		readonly IWindowNativeApi windowNativeApi;

		readonly IDictionary<IntPtr, byte[]> dataSourceIconCache;

		public DataSourceService(
			IImagePersistenceService imagePersistenceService,
			IWindowNativeApi windowNativeApi)
		{
			this.imagePersistenceService = imagePersistenceService;
			this.windowNativeApi = windowNativeApi;

			dataSourceIconCache = new Dictionary<IntPtr, byte[]>();
		}

		BitmapSource GetWindowIcon(IntPtr windowHandle)
		{
			var hIcon = windowNativeApi.SendMessage(
				windowHandle,
				(int)Message.WM_GETICON,
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
			lock (this)
			{
				byte[] iconBytes;
				if (dataSourceIconCache.ContainsKey(activeWindowHandle)) {
					iconBytes = dataSourceIconCache[activeWindowHandle];
				} else
				{
					var windowIcon = GetWindowIcon(activeWindowHandle);
					iconBytes = imagePersistenceService.ConvertBitmapSourceToByteArray(windowIcon);
					dataSourceIconCache.Add(activeWindowHandle, iconBytes);
				}

				var dataSource = new DataSource(iconBytes, windowTitle);
				return dataSource;
			}
		}
	}
}
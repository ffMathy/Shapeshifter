namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
    using System;
	using System.ComponentModel;
	using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading.Tasks;

    using Data;
    using Data.Interfaces;
    using Images.Interfaces;
    using Infrastructure.Dependencies.Interfaces;
    using Interfaces;

    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using Windows.ApplicationModel;
    using Windows.Management.Deployment;
    using Windows.Storage.Streams;

    using Infrastructure.Caching.Interfaces;
    using Infrastructure.Threading.Interfaces;

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

        public async Task<IDataSource> GetDataSourceAsync()
        {
            var activeWindowHandle = activeWindowService.ActiveWindowHandle;
            var activeWindowTitle = activeWindowService.GetWindowTitleFromWindowHandle(activeWindowHandle);

            var process = activeWindowService.GetProcessFromWindowHandle(activeWindowHandle);
            var processName = process.ProcessName + ".exe";

            var iconBytesBig = dataSourceIconCacheLarge.Get(activeWindowHandle);
            if (iconBytesBig == null)
            {
                var windowIconBig = await GetWindowIconAsync(process, activeWindowHandle);
                iconBytesBig = imagePersistenceService.ConvertBitmapSourceToByteArray(windowIconBig);

                dataSourceIconCacheLarge.Set(activeWindowHandle, iconBytesBig);
            }

            var iconBytesSmall = dataSourceIconCacheSmall.Get(activeWindowHandle);
            if (iconBytesSmall == null)
            {
                var windowIconSmall = await GetWindowIconAsync(process, activeWindowHandle, false);
                iconBytesSmall = imagePersistenceService.ConvertBitmapSourceToByteArray(windowIconSmall);

                dataSourceIconCacheSmall.Set(activeWindowHandle, iconBytesSmall);
            }

            var dataSource = new DataSource(
                iconBytesBig,
                iconBytesSmall,
                activeWindowTitle,
                processName);
            return dataSource;
        }

        async Task<BitmapSource> GetWindowIconAsync(
            Process process,
            IntPtr windowHandle,
            bool bigIconSize = true)
        {
            if (process.Id != 0)
			{
				try
				{
					var processDirectoryName = Path.GetFileName(Path.GetDirectoryName(process.MainModule.FileName));
					Debug.Assert(processDirectoryName != null, nameof(processDirectoryName) + " != null");

					var universalWindowsApplicationPackage = packageManager.FindPackageForUser(string.Empty, processDirectoryName);
					if (universalWindowsApplicationPackage != null)
						return await GetIconFromUniversalWindowsApplicationAsync(
							universalWindowsApplicationPackage,
							new Size(1024, 1024));
				}
				catch (Win32Exception)
				{
					//issue #560 trying to fetch MainModule of a 32 bit process from Shapeshifter which is a 64 bit process.
				}
			}

            var hIcon = windowNativeApi.SendMessage(
                windowHandle,
                (int)Message.WM_GETICON,
                bigIconSize ? WindowNativeApi.ICON_LARGE : WindowNativeApi.ICON_SMALL,
                IntPtr.Zero);

            if (hIcon == IntPtr.Zero)
                hIcon = windowNativeApi.GetClassLongPtr(windowHandle, WindowNativeApi.GCL_HICON);

            if (hIcon == IntPtr.Zero)
                hIcon = windowNativeApi.LoadIcon(IntPtr.Zero, WindowNativeApi.IDI_APPLICATION);

            if (hIcon != IntPtr.Zero)
            {
                return Imaging.CreateBitmapSourceFromHIcon(
                    hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }

            throw new InvalidOperationException("Could not load window icon.");
        }

        async Task<BitmapSource> GetIconFromUniversalWindowsApplicationAsync(Package package, Size size)
        {
            var activeWindowTitle = activeWindowService.ActiveWindowTitle;
            var titleRegions = activeWindowTitle.Split(new[] { " - " }, StringSplitOptions.None);

            var appListEntries = (await package.GetAppListEntriesAsync()
                .AsTask()).ToArray();
            var appListEntry = appListEntries.FirstOrDefault(x => x
				.DisplayInfo
                .DisplayName
                .EndsWith(titleRegions.Last())) ?? appListEntries.First();

            var resource = appListEntry.DisplayInfo.GetLogo(new Windows.Foundation.Size(size.Width, size.Height));
			
			using (var randomAccessStream = await resource.OpenReadAsync().AsTask())
			using (var stream = randomAccessStream.AsStream())
			{
				var bitmap = new BitmapImage();
				bitmap.BeginInit();
				bitmap.DecodePixelWidth = (int)size.Width;
				bitmap.DecodePixelHeight = (int)size.Height;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
				bitmap.StreamSource = stream;
				bitmap.EndInit();

				return bitmap;
			}
        }
    }
}

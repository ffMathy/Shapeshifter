using WindowsClipboard = System.Windows.Clipboard;

namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Threading.Tasks;
	using System.Windows.Media.Imaging;

	using Data.Interfaces;

	using Infrastructure.Handles.Factories.Interfaces;
	using Infrastructure.Handles.Interfaces;
	using Infrastructure.Threading.Interfaces;

	using Interfaces;

	using Messages.Interceptors.Interfaces;

	using Serilog;

	using Services.Interfaces;

	using Shapeshifter.WindowsDesktop.Data.Wrappers.Interfaces;

	class ClipboardInjectionService : IClipboardInjectionService
	{
		readonly IThreadDelay threadDelay;
		readonly IClipboardCopyInterceptor clipboardCopyInterceptor;
		readonly IClipboardHandleFactory clipboardHandleFactory;
		readonly ILogger logger;
		readonly IEnumerable<IMemoryWrapper> memoryWrappers;

		public ClipboardInjectionService(
			IThreadDelay threadDelay,
			IClipboardCopyInterceptor clipboardCopyInterceptor,
			IClipboardHandleFactory clipboardHandleFactory,
			ILogger logger,
			IEnumerable<IMemoryWrapper> memoryWrappers)
		{
			this.threadDelay = threadDelay;
			this.clipboardCopyInterceptor = clipboardCopyInterceptor;
			this.clipboardHandleFactory = clipboardHandleFactory;
			this.logger = logger;
			this.memoryWrappers = memoryWrappers;
		}

		public async Task InjectDataAsync(IClipboardDataPackage package)
		{
			try
			{
				clipboardCopyInterceptor.SkipNext();

				using (var session = clipboardHandleFactory.StartNewSession())
				{ 
					session.EmptyClipboard();
					InjectPackageContents(session, package);
				}

				logger.Information("Clipboard package has been injected to the clipboard.", 1);
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Could not inject clipboard data.");
				throw;
			}
		}

		void InjectPackageContents(
			IClipboardHandle session,
			IClipboardDataPackage package)
		{
			foreach (var clipboardData in package.Contents)
			{
				InjectClipboardData(session, clipboardData);
			}
		}

		void InjectClipboardData(
			IClipboardHandle session,
			IClipboardData clipboardData)
		{
			var wrappers = memoryWrappers
				.Where(x => x.CanWrap(clipboardData))
				.ToArray();
			if (wrappers.Length > 0)
			{
				logger.Verbose("Injecting {bytes} bytes of {format} format into the clipboard.", clipboardData.RawData.Length, clipboardData.RawFormat);
			}
			else
			{
				logger.Verbose("No suitable memory wrapper found for format {format}.", clipboardData.RawFormat);
			}

			foreach (var wrapper in wrappers)
			{
				var success = session.SetClipboardData(
					clipboardData.RawFormat.Number,
					wrapper.GetDataPointer(
						clipboardData));
				if (success == IntPtr.Zero && session.OpenedSuccessfully)
				{
					throw new Exception(
						"Could not set clipboard data format " + clipboardData.RawFormat + " from " + clipboardData.Package.Source.ProcessName + ".",
						new Win32Exception(Marshal.GetLastWin32Error()));
				}

				var formats = session.GetClipboardFormats();
				if (formats.All(x => x.Number != clipboardData.RawFormat.Number))
				{
					logger.Warning(
						"The format {format} was not found in the clipboard after attempting injecting it to the clipboard.", 
						clipboardData.RawFormat);
				}
			}
		}

		public async Task InjectImageAsync(BitmapSource image)
		{
			clipboardCopyInterceptor.SkipNext();
			WindowsClipboard.SetImage(image);
		}

		public async Task InjectTextAsync(string text)
		{
			clipboardCopyInterceptor.SkipNext();
			WindowsClipboard.SetText(text);
		}

		public async Task InjectFilesAsync(params string[] files)
		{
			clipboardCopyInterceptor.SkipNext();

			var collection = new StringCollection();
			collection.AddRange(files);

			WindowsClipboard.SetFileDropList(collection);
		}

		public void ClearClipboard()
		{
			WindowsClipboard.Clear();
		}
	}
}
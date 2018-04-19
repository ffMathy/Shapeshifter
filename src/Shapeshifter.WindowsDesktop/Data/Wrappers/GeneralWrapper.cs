using System;
using System.Linq;
using Shapeshifter.WindowsDesktop.Data.Interfaces;
using Shapeshifter.WindowsDesktop.Infrastructure.Handles.Factories.Interfaces;
using Shapeshifter.WindowsDesktop.Native;
using Shapeshifter.WindowsDesktop.Native.Interfaces;

namespace Shapeshifter.WindowsDesktop.Data.Wrappers
{
	class GeneralWrapper : IGeneralWrapper
	{
		readonly IMemoryHandleFactory memoryHandleFactory;
		readonly IGeneralNativeApi generalNativeApi;

		readonly int[] excludedFormats;

		public GeneralWrapper(
			IMemoryHandleFactory memoryHandleFactory,
			IGeneralNativeApi generalNativeApi)
		{
			this.memoryHandleFactory = memoryHandleFactory;
			this.generalNativeApi = generalNativeApi;

			excludedFormats = new[]
			{
				ClipboardNativeApi.CF_DSPBITMAP,
				ClipboardNativeApi.CF_DSPENHMETAFILE,
				ClipboardNativeApi.CF_ENHMETAFILE,
				ClipboardNativeApi.CF_METAFILEPICT,
				ClipboardNativeApi.CF_BITMAP
			};
		}

		public bool CanWrap(IClipboardData clipboardData)
		{
			return excludedFormats.All(x => x != clipboardData.RawFormat);
		}

		public IntPtr GetDataPointer(IClipboardData clipboardData)
		{
			using (var memoryHandle = memoryHandleFactory.AllocateInMemory(clipboardData.RawData))
			{
				var globalPointer = AllocateInMemory(clipboardData);

				var target = generalNativeApi.GlobalLock(globalPointer);
				if (target == IntPtr.Zero)
				{
					throw new InvalidOperationException("Could not allocate memory.");
				}

				generalNativeApi.CopyMemory(
					target,
					memoryHandle.Pointer,
					(uint)clipboardData.RawData.Length);

				generalNativeApi.GlobalUnlock(target);

				return globalPointer;
			}
		}

		IntPtr AllocateInMemory(IClipboardData clipboardData)
		{
			return generalNativeApi.GlobalAlloc(
				GeneralNativeApi.GMEM_ZEROINIT | GeneralNativeApi.GMEM_MOVABLE,
				(UIntPtr)clipboardData.RawData.Length);
		}
	}
}

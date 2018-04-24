using Shapeshifter.WindowsDesktop.Data.Interfaces;
using System;

namespace Shapeshifter.WindowsDesktop.Data.Wrappers.Interfaces
{
	public interface IMemoryWrapper
	{
		bool CanWrap(IClipboardData clipboardData);
		IntPtr GetDataPointer(IClipboardData clipboardData);
	}
}

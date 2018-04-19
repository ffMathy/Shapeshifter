using Shapeshifter.WindowsDesktop.Data.Interfaces;
using System;

namespace Shapeshifter.WindowsDesktop.Data.Wrappers
{
	public interface IMemoryWrapper
	{
		bool CanWrap(IClipboardData clipboardData);
		IntPtr GetDataPointer(IClipboardData clipboardData);
	}
}

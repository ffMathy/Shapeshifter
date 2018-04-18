using Shapeshifter.WindowsDesktop.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Data.Wrappers
{
	public interface IMemoryWrapper
	{
		bool CanWrap(IClipboardData clipboardData);
		IntPtr GetDataPointer(IClipboardData clipboardData);
	}
}

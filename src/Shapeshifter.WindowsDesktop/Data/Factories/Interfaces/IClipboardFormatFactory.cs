using Shapeshifter.WindowsDesktop.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Data.Factories.Interfaces
{
	public interface IClipboardFormatFactory
	{
		IClipboardFormat Create(uint format);
	}
}

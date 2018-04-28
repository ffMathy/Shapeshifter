using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Data.Interfaces
{
	public interface IClipboardFormat
	{
		uint Number { get; }
		string Name { get; }
	}
}

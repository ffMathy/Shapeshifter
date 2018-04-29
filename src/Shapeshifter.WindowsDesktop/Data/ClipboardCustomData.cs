using Shapeshifter.WindowsDesktop.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Data
{
	class ClipboardCustomData : IClipboardCustomData
	{
		public byte[] RawData { get; set; }

		public IClipboardFormat RawFormat { get; set; }
		public IClipboardDataPackage Package { get; set; }
	}
}

using Shapeshifter.WindowsDesktop.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Data
{
	class ClipboardFormat : IClipboardFormat
	{
		public uint Number { get; set; }
		public string Name { get; set; }

		public override string ToString()
		{
			if(Name != null)
				return "[" + Name + "#" + Number + "]";

			return Number.ToString();
		}
	}
}

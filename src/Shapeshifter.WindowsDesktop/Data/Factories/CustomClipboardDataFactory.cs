using Shapeshifter.WindowsDesktop.Data.Factories.Interfaces;
using Shapeshifter.WindowsDesktop.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Data.Factories
{
	class CustomClipboardDataFactory : ICustomClipboardDataFactory
	{
		public IClipboardData BuildData(IClipboardFormat format, byte[] rawData)
		{
			return new ClipboardCustomData() {
				RawData = rawData,
				RawFormat = format
			};
		}

		public bool CanBuildData(IClipboardFormat format)
		{
			return true;
		}
	}
}

using Shapeshifter.WindowsDesktop.Data.Interfaces;

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

using Shapeshifter.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.WindowsDesktop.Data.Factories.Interfaces
{
	public interface IClipboardFormatFactory
	{
		IClipboardFormat Create(uint format);
	}
}

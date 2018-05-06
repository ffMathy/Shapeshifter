using Shapeshifter.WindowsDesktop.Data.Factories.Interfaces;
using Shapeshifter.WindowsDesktop.Data.Interfaces;
using Shapeshifter.WindowsDesktop.Native.Interfaces;

namespace Shapeshifter.WindowsDesktop.Data.Factories
{
	class ClipboardFormatFactory : IClipboardFormatFactory
	{
		readonly IClipboardNativeApi clipboardNativeApi;

		public ClipboardFormatFactory(IClipboardNativeApi clipboardNativeApi)
		{
			this.clipboardNativeApi = clipboardNativeApi;
		}

		public IClipboardFormat Create(uint format)
		{
			return new ClipboardFormat() {
				Name = clipboardNativeApi.GetClipboardFormatName(format),
				Number = format
			};
		}
	}
}

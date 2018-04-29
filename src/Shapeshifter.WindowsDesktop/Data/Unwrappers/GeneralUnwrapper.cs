namespace Shapeshifter.WindowsDesktop.Data.Unwrappers
{
    using System.Collections.Generic;
    using System.Linq;

    using Interfaces;

    using Native;
    using Native.Interfaces;
	using Shapeshifter.WindowsDesktop.Data.Interfaces;

	class GeneralUnwrapper: IGeneralUnwrapper
    {
        readonly IClipboardNativeApi clipboardNativeApi;

		public GeneralUnwrapper(
            IClipboardNativeApi clipboardNativeApi)
        {
            this.clipboardNativeApi = clipboardNativeApi;
        }

        public bool CanUnwrap(IClipboardFormat format)
        {
			if(format.Name == "DataObject")
				return false;

            return true;
        }

        public byte[] UnwrapStructure(IClipboardFormat format)
        {
            return clipboardNativeApi.GetClipboardDataBytes(format.Number);
        }
    }
}
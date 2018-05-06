namespace Shapeshifter.WindowsDesktop.Data.Unwrappers
{
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

			//applications like Word can throw some nasty metafiles into the clipboard causing a fatal crash.
			if (format.Number == ClipboardNativeApi.CF_ENHMETAFILE || format.Number == ClipboardNativeApi.CF_METAFILEPICT)
				return false;

            return true;
        }

        public byte[] UnwrapStructure(IClipboardFormat format)
        {
            return clipboardNativeApi.GetClipboardDataBytes(format.Number);
        }
    }
}
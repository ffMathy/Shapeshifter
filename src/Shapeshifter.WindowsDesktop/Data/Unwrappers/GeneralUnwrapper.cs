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

        readonly IEnumerable<int> excludedFormats;

        public GeneralUnwrapper(
            IClipboardNativeApi clipboardNativeApi)
        {
            this.clipboardNativeApi = clipboardNativeApi;
            excludedFormats = new[]
            {
                ClipboardNativeApi.CF_DSPBITMAP,
                ClipboardNativeApi.CF_DSPENHMETAFILE,
                ClipboardNativeApi.CF_ENHMETAFILE,
                ClipboardNativeApi.CF_METAFILEPICT,
                ClipboardNativeApi.CF_BITMAP
            };
        }

        public bool CanUnwrap(IClipboardFormat format)
        {
            return excludedFormats.All(x => x != format.Number);
        }

        public byte[] UnwrapStructure(IClipboardFormat format)
        {
            return clipboardNativeApi.GetClipboardDataBytes(format.Number);
        }
    }
}
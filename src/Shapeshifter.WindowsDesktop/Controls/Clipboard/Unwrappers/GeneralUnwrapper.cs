namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Unwrappers
{
    using System.Collections.Generic;
    using System.Linq;

    using Interfaces;

    using Native;

    class GeneralUnwrapper: IMemoryUnwrapper
    {
        readonly IEnumerable<int> excludedFormats;

        public GeneralUnwrapper()
        {
            excludedFormats = new[]
            {
                ClipboardApi.CF_DSPBITMAP,
                ClipboardApi.CF_DSPENHMETAFILE,
                ClipboardApi.CF_ENHMETAFILE,
                ClipboardApi.CF_METAFILEPICT,
                ClipboardApi.CF_BITMAP
            };
        }

        public bool CanUnwrap(uint format)
        {
            return excludedFormats.All(x => x != format);
        }

        public byte[] UnwrapStructure(uint format)
        {
            return ClipboardApi.GetClipboardDataBytes(format);
        }
    }
}
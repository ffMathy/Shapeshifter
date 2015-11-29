namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Unwrappers
{
    using System.Collections.Generic;
    using System.Linq;

    using Api;

    using Interfaces;

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
                ClipboardApi.CF_METAFILEPICT
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
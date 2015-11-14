#region

using System.Collections.Generic;
using System.Linq;
using Shapeshifter.UserInterface.WindowsDesktop.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Unwrappers.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Unwrappers
{
    internal class GeneralUnwrapper : IMemoryUnwrapper
    {
        private readonly IEnumerable<int> excludedFormats;

        public GeneralUnwrapper()
        {
            excludedFormats = new[]
            {
                ClipboardApi.CF_BITMAP,
                ClipboardApi.CF_DIB,
                ClipboardApi.CF_DIBV5,
                ClipboardApi.CF_DSPBITMAP,
                ClipboardApi.CF_DSPENHMETAFILE,
                ClipboardApi.CF_ENHMETAFILE,
                ClipboardApi.CF_METAFILEPICT
            };
        }

        public bool CanUnwrap(uint format)
        {
            return !excludedFormats.Any(x => x == format);
        }

        public byte[] UnwrapStructure(uint format)
        {
            return ClipboardApi.GetClipboardDataBytes(format);
        }
    }
}
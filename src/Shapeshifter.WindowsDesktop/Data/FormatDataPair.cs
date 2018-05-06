using Shapeshifter.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.WindowsDesktop.Structures
{
    public struct FormatDataPair
    {
        public FormatDataPair(IClipboardFormat format, byte[] data)
        {
            Format = format;
            Data = data;
        }

        public IClipboardFormat Format { get; set; }

        public byte[] Data { get; set; }
    }
}
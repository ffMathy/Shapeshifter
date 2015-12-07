namespace Shapeshifter.UserInterface.WindowsDesktop.Structures
{
    public struct FormatDataPair
    {
        public FormatDataPair(uint format, byte[] data)
        {
            Format = format;
            Data = data;
        }

        public uint Format { get; set; }

        public byte[] Data { get; set; }
    }
}

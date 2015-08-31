namespace Shapeshifter.Core.Data.Interfaces
{
    public interface IClipboardImageData : IClipboardData
    {
        byte[] Image { get; }
    }
}

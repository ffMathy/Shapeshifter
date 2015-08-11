namespace Shapeshifter.Core.Data.Interfaces
{
    public interface IClipboardFileData : IClipboardData
    {
        string FileName { get; set; }
        byte[] FileIcon { get; set; }
    }
}

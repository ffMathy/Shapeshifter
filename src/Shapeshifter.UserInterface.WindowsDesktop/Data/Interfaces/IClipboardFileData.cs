namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces
{
    public interface IClipboardFileData: IClipboardData
    {
        string FileName { get; set; }

        string FullPath { get; set; }

        byte[] FileIcon { get; set; }
    }
}
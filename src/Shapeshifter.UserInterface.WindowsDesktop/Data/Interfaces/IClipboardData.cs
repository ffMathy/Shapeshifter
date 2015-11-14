namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces
{
    public interface IClipboardData
    {
        IDataSource Source { get; }

        byte[] RawData { get; }

        uint RawFormat { get; }
    }
}
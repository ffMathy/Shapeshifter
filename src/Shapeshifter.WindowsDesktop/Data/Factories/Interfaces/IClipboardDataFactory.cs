namespace Shapeshifter.WindowsDesktop.Data.Factories.Interfaces
{
    using Data.Interfaces;

    public interface IClipboardDataFactory
    {
        bool CanBuildData(IClipboardFormat format);

        IClipboardData BuildData(IClipboardFormat format, byte[] rawData);
    }
}
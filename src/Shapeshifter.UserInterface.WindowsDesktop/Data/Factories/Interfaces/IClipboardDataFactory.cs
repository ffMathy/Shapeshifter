namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Factories.Interfaces
{
    using Data.Interfaces;

    public interface IClipboardDataFactory
    {
        bool CanBuildData(uint format);

        IClipboardData BuildData(uint format, byte[] rawData);
    }
}

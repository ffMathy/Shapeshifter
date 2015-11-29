namespace Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces
{
    using Controls.Clipboard.Interfaces;

    using Data.Interfaces;

    public interface IClipboardDataControlFactory
    {
        bool CanBuildData(uint format);

        bool CanBuildControl(IClipboardData data);

        IClipboardControl BuildControl(IClipboardData clipboardData);

        IClipboardData BuildData(uint format, byte[] rawData);
    }
}
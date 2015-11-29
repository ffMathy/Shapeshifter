namespace Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces
{
    using Controls.Clipboard.Interfaces;

    using Data.Interfaces;

    public interface IClipboardDataControlFactory
    {
        bool CanBuildControl(IClipboardData data);

        IClipboardControl BuildControl(IClipboardData clipboardData);
    }
}
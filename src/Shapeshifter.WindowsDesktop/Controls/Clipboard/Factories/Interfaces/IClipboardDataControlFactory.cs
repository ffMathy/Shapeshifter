namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Factories.Interfaces
{
    using Clipboard.Interfaces;

    using Data.Interfaces;

    public interface IClipboardDataControlFactory
    {
        bool CanBuildControl(IClipboardData data);

        IClipboardControl BuildControl(IClipboardData clipboardData);
    }
}
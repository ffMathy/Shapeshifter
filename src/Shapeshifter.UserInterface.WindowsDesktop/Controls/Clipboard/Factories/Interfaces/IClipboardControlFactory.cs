namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces
{
    using Clipboard.Interfaces;

    using Data.Interfaces;

    public interface IClipboardControlFactory<in TData, out TControl>
        where TData : IClipboardData
        where TControl : IClipboardControl
    {
        TControl CreateControl(TData data);
    }
}
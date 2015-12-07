namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.Interfaces
{
    using Data.Interfaces;

    public interface IClipboardDataViewModel<TClipboardData>
        where TClipboardData : IClipboardData
    {
        TClipboardData Data { get; set; }
    }
}
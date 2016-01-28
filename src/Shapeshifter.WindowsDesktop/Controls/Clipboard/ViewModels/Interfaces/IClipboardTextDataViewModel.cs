namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.ViewModels.Interfaces
{
    using Data.Interfaces;

    public interface IClipboardTextDataViewModel: IClipboardDataViewModel<IClipboardTextData>
    {
        string FriendlyText { get; } 
    }
}
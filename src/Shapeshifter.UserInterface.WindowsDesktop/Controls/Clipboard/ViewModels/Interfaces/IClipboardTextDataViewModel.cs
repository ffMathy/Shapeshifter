using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.Interfaces
{
    public interface IClipboardTextDataViewModel : IClipboardDataViewModel<IClipboardTextData>
    {
        string FriendlyText { get; }
    }
}
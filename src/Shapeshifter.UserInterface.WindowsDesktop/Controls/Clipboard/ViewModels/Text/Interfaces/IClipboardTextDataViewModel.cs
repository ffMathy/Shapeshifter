using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.Text.Interfaces
{
    public interface IClipboardTextDataViewModel : IClipboardDataViewModel<IClipboardTextData>
    {
        string FriendlyText { get; }
    }
}

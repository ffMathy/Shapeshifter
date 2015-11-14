#region

using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.Interfaces
{
    public interface IClipboardTextDataViewModel : IClipboardDataViewModel<IClipboardTextData>
    {
        string FriendlyText { get; }
    }
}
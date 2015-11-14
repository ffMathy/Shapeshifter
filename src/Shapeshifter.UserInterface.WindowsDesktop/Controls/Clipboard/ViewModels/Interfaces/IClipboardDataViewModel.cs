#region

using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.Interfaces
{
    public interface IClipboardDataViewModel<TClipboardData>
        where TClipboardData : IClipboardData
    {
        TClipboardData Data { get; set; }
    }
}
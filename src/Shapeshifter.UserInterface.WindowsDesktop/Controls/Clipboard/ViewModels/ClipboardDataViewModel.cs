#region

using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    internal abstract class ClipboardDataViewModel<TClipboardData> :
        IClipboardDataViewModel<TClipboardData>
        where TClipboardData : IClipboardData
    {
        public TClipboardData Data { get; set; }
    }
}
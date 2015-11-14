using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    abstract class ClipboardDataViewModel<TClipboardData> :
        IClipboardDataViewModel<TClipboardData>
        where TClipboardData : IClipboardData
    {
        protected ClipboardDataViewModel() { }

        public TClipboardData Data { get; set; }
    }
}

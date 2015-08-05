using Shapeshifter.Core.Data;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    abstract class ClipboardDataViewModel<TClipboardData> where TClipboardData : IClipboardData
    {
        protected ClipboardDataViewModel() { }

        protected ClipboardDataViewModel(TClipboardData data)
        {
            Data = data;
        }

        public TClipboardData Data { get; protected set; }
    }
}

namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.ViewModels
{
    using Data.Interfaces;

    using Interfaces;

    abstract class ClipboardDataViewModel<TClipboardData>:
        IClipboardDataViewModel<TClipboardData>
        where TClipboardData : IClipboardData
    {
        public TClipboardData Data { get; set; }
    }
}
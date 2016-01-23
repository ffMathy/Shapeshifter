namespace Shapeshifter.WindowsDesktop.Services.Clipboard.Interfaces
{
    using System.Threading.Tasks;

    public interface IClipboardPasteService
    {
        Task PasteClipboardContentsAsync();
    }
}
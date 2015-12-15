using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Services.Clipboard.Interfaces
{
    public interface IClipboardPasteService
    {
        Task PasteClipboardContentsAsync();
    }
}
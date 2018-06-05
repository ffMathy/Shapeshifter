namespace Shapeshifter.WindowsDesktop.Services.Clipboard.Interfaces
{
    using System.Threading.Tasks;

	using Infrastructure.Dependencies.Interfaces;

    public interface IClipboardPasteService: ISingleInstance
    {
        Task PasteClipboardContentsAsync();
    }
}
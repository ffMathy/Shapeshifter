using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface IFileIconService
    {
        [ExcludeFromCodeCoverage]
        byte[] GetIcon(string path, bool allowThumbnails, int dimensions = 256);
    }
}

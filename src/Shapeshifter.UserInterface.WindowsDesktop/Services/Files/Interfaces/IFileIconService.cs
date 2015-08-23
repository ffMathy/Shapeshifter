using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface IFileIconService : ISingleInstance
    {
        [ExcludeFromCodeCoverage]
        byte[] GetIcon(string path, bool allowThumbnails, int dimensions = 256);
    }
}

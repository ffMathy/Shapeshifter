#region

using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces
{
    public interface IFileIconService : ISingleInstance
    {
        [ExcludeFromCodeCoverage]
        byte[] GetIcon(string path, bool allowThumbnails, int dimensions = 256);
    }
}
namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces
{
    using System.Diagnostics.CodeAnalysis;

    using Infrastructure.Dependencies.Interfaces;

    public interface IFileIconService: ISingleInstance
    {
        [ExcludeFromCodeCoverage]
        byte[] GetIcon(string path, bool allowThumbnails, int dimensions = 256);
    }
}
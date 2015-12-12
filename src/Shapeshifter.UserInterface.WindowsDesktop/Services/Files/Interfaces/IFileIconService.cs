namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces
{
    using System.Diagnostics.CodeAnalysis;

    using Infrastructure.Dependencies.Interfaces;

    public interface IFileIconService: ISingleInstance
    {
        
        byte[] GetIcon(string path, bool allowThumbnails, int dimensions = 256);
    }
}
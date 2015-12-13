namespace Shapeshifter.WindowsDesktop.Services.Files.Interfaces
{
    public interface IFileTypeInterpreter
    {
        FileType GetFileTypeFromFileName(string name);
    }
}
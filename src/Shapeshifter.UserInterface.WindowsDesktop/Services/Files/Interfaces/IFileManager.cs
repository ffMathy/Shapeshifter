namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    interface IFileManager
    {
        string WriteBytesToTemporaryFile(string path, byte[] bytes);

        string PrepareTemporaryPath(string path);
    }
}

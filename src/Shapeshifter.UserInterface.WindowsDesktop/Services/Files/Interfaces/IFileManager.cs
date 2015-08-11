namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    interface IFileManager
    {
        void WriteBytesToFile(string path, byte[] bytes);

        string PrepareTemporaryPath(string path);
    }
}

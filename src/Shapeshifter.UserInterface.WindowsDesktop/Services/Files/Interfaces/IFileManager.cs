namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    interface IFileManager
    {
        void WriteBytesToTemporaryFile(string path, byte[] bytes);

        string PrepareTemporaryPath(string path);
    }
}

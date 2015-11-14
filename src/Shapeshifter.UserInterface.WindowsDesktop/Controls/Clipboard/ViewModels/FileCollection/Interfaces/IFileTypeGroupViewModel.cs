namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection.Interfaces
{
    public interface IFileTypeGroupViewModel
    {
        int Count { get; }
        string FileType { get; }
        byte[] Icon { get; }
    }
}
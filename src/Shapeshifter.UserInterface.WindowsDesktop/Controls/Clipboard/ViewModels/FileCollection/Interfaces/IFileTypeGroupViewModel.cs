namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection
{
    public interface IFileTypeGroupViewModel
    {
        int Count { get; }
        string FileType { get; }
        byte[] Icon { get; }
    }
}
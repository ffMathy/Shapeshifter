namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection.
    Interfaces
{
    using System.Collections.Generic;

    using Data.Interfaces;

    using ViewModels.Interfaces;

    public interface IClipboardFileCollectionDataViewModel
        : IClipboardDataViewModel<IClipboardFileCollectionData>
    {
        int FileCount { get; }

        IEnumerable<IFileTypeGroupViewModel> FileTypeGroups { get; }
    }
}
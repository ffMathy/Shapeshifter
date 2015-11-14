using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection.Interfaces
{
    public interface IClipboardFileCollectionDataViewModel : IClipboardDataViewModel<IClipboardFileCollectionData>
    {
        int FileCount { get; }

        IEnumerable<IFileTypeGroupViewModel> FileTypeGroups { get; }
    }
}
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.Interfaces;
using System.Collections.Generic;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection.Interfaces
{
    public interface IClipboardFileCollectionDataViewModel : IClipboardDataViewModel<IClipboardFileCollectionData>
    {
        int FileCount { get; }

        IEnumerable<IFileTypeGroupViewModel> FileTypeGroups { get; }
    }
}

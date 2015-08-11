using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection;
using Shapeshifter.Core.Data.Interfaces;
using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    class ClipboardFileCollectionDataViewModel : ClipboardDataViewModel<IClipboardFileCollectionData>,
        IClipboardFileCollectionDataViewModel
    {

        public ClipboardFileCollectionDataViewModel()
        {
            PrepareDesignerMode();
        }

        [ExcludeFromCodeCoverage]
        private void PrepareDesignerMode()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Data = new DesignerClipboardFileCollectionDataFacade();
            }
        }

        public int FileCount
        {
            get
            {
                return Data.Files.Count();
            }
        }

        public IEnumerable<IFileTypeGroupViewModel> FileTypeGroups
        {
            get
            {
                return Data
                    .Files
                    .GroupBy(x => Path.GetExtension(x.FileName))
                    .OrderBy(x => x.Count())
                    .Select(x => new FileTypeGroupViewModel(x));
            }
        }
    }
}

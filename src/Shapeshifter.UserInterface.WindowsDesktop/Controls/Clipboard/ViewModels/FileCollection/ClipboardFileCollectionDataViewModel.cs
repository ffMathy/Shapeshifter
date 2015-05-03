using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    class ClipboardFileCollectionDataViewModel : ClipboardDataViewModel<ClipboardFileCollectionData>
    {

        public ClipboardFileCollectionDataViewModel()
        {
            if(DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Data = new DesignerClipboardFileCollectionDataFacade();
            }
        }

        public ClipboardFileCollectionDataViewModel(ClipboardFileCollectionData data) : base(data)
        {
        }

        public int FileCount
        {
            get
            {
                return Data.Files.Count();
            }
        }

        public IEnumerable<FileTypeGroupViewModel> FileTypeGroups
        {
            get
            {
                return Data
                    .Files
                    .GroupBy(x => Path.GetExtension(x.FileName))
                    .Select(x => new FileTypeGroupViewModel(x));
            }
        }
    }
}

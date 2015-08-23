using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection;
using Shapeshifter.Core.Data.Interfaces;
using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection.Interfaces;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Helpers;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    class ClipboardFileCollectionDataViewModel : ClipboardDataViewModel<IClipboardFileCollectionData>,
        IClipboardFileCollectionDataViewModel
    {

        public ClipboardFileCollectionDataViewModel(
            IEnvironmentInformation environmentInformation)
        {
            if (environmentInformation.IsInDesignTime)
            {
                PrepareDesignerMode();
            }
        }

        [ExcludeFromCodeCoverage]
        void PrepareDesignerMode()
        {
            var container = DesignTimeContainerHelper.CreateDesignTimeContainer();
            Data = container.Resolve<DesignerClipboardFileCollectionDataFacade>();
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
                    .OrderByDescending(x => x.Count())
                    .Select(x => new FileTypeGroupViewModel(x));
            }
        }
    }
}

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Facades;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Helpers;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection
{
    internal class ClipboardFileCollectionDataViewModel : ClipboardDataViewModel<IClipboardFileCollectionData>,
        IClipboardFileCollectionDataViewModel
    {
        [ExcludeFromCodeCoverage]
        public ClipboardFileCollectionDataViewModel() : this(new EnvironmentInformation())
        {
        }

        public ClipboardFileCollectionDataViewModel(
            IEnvironmentInformation environmentInformation)
        {
            if (environmentInformation.IsInDesignTime)
            {
                PrepareDesignerMode();
            }
        }

        [ExcludeFromCodeCoverage]
        private void PrepareDesignerMode()
        {
            var container = DesignTimeContainerHelper.CreateDesignTimeContainer();
            Data = container.Resolve<DesignerClipboardFileCollectionDataFacade>();
        }

        public int FileCount => Data.Files.Count;

        public IEnumerable<IFileTypeGroupViewModel> FileTypeGroups
        {
            get
            {
                return Data
                    .Files
                    .GroupBy(x => Path.GetExtension(x.FileName))
                    .OrderByDescending(x => x.Count())
                    .Select(x => new FileTypeGroupViewModel(x.ToArray()));
            }
        }
    }
}
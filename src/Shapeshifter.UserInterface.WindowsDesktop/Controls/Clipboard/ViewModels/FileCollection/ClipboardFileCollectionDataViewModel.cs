namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Autofac;

    using Data.Interfaces;

    using Designer.Facades;
    using Designer.Helpers;

    using Infrastructure.Environment;
    using Infrastructure.Environment.Interfaces;

    using Interfaces;

    class ClipboardFileCollectionDataViewModel
        : ClipboardDataViewModel<IClipboardFileCollectionData>,
          IClipboardFileCollectionDataViewModel
    {
        
        public ClipboardFileCollectionDataViewModel()
            : this(new EnvironmentInformation()) { }

        public ClipboardFileCollectionDataViewModel(
            IEnvironmentInformation environmentInformation)
        {
            if (environmentInformation.IsInDesignTime)
            {
                PrepareDesignerMode();
            }
        }

        
        void PrepareDesignerMode()
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
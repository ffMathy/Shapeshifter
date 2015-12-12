namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    using System.Diagnostics.CodeAnalysis;

    using Autofac;

    using Data.Interfaces;

    using Designer.Facades;
    using Designer.Helpers;

    using Infrastructure.Environment;
    using Infrastructure.Environment.Interfaces;

    
    class ClipboardFileDataViewModel: ClipboardDataViewModel<IClipboardFileData>
    {
        public ClipboardFileDataViewModel()
            : this(new EnvironmentInformation()) { }

        public ClipboardFileDataViewModel(
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
            Data = container.Resolve<DesignerClipboardFileDataFacade>();
        }
    }
}
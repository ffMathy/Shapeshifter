namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.ViewModels
{
    using Autofac;

    using Data.Interfaces;

    using Designer.Facades;
    using Designer.Helpers;

    using Infrastructure.Environment;
    using Infrastructure.Environment.Interfaces;

    class ClipboardFileDataViewModel: ClipboardDataViewModel<IClipboardFileData>
    {
        public ClipboardFileDataViewModel()
            : this(new EnvironmentInformation(true)) { }

        public ClipboardFileDataViewModel(
            IEnvironmentInformation environmentInformation)
        {
            if (environmentInformation.GetIsInDesignTime())
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
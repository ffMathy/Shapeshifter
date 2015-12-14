namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.ViewModels
{
    using Autofac;

    using Data.Interfaces;

    using Designer.Facades;
    using Designer.Helpers;

    using Infrastructure.Environment;
    using Infrastructure.Environment.Interfaces;

    class ClipboardImageDataViewModel: ClipboardDataViewModel<IClipboardImageData>
    {
        public ClipboardImageDataViewModel()
            : this(new EnvironmentInformation()) { }

        public ClipboardImageDataViewModel(
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
            Data = container.Resolve<DesignerClipboardImageDataFacade>();
        }
    }
}
using Autofac;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Helpers;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    class ClipboardImageDataViewModel : ClipboardDataViewModel<IClipboardImageData>
    {
        public ClipboardImageDataViewModel() : this(new EnvironmentInformation()) { }

        public ClipboardImageDataViewModel(
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
            Data = container.Resolve<DesignerClipboardImageDataFacade>();
        }
    }
}

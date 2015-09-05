using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer;
using Shapeshifter.Core.Data.Interfaces;
using System.Diagnostics.CodeAnalysis;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Helpers;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    class ClipboardFileDataViewModel : ClipboardDataViewModel<IClipboardFileData>
    {
        public ClipboardFileDataViewModel() : this(new EnvironmentInformation()) { }

        public ClipboardFileDataViewModel(
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
            Data = container.Resolve<DesignerClipboardFileDataFacade>();
        }
    }
}

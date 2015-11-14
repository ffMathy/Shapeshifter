#region

using System.Diagnostics.CodeAnalysis;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Facades;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Helpers;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    internal class ClipboardFileDataViewModel : ClipboardDataViewModel<IClipboardFileData>
    {
        public ClipboardFileDataViewModel() : this(new EnvironmentInformation())
        {
        }

        public ClipboardFileDataViewModel(
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
            Data = container.Resolve<DesignerClipboardFileDataFacade>();
        }
    }
}
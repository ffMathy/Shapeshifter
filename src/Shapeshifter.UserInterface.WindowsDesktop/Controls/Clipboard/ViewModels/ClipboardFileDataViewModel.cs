using System.ComponentModel;
using System.Windows;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer;
using Shapeshifter.Core.Data.Interfaces;
using System.Diagnostics.CodeAnalysis;
using Autofac;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    class ClipboardFileDataViewModel : ClipboardDataViewModel<IClipboardFileData>
    {
        public ClipboardFileDataViewModel()
        {
            PrepareDesignerMode();
        }

        [ExcludeFromCodeCoverage]
        private void PrepareDesignerMode()
        {
            if (App.InDesignMode)
            {
                Data = App.Container.Resolve<DesignerClipboardFileDataFacade>();
            }
        }
    }
}

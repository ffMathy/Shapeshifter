using System.ComponentModel;
using System.Windows;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    class ClipboardFileDataViewModel : ClipboardDataViewModel<ClipboardFileData>
    {
        public ClipboardFileDataViewModel()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Data = new DesignerClipboardFileDataFacade();
            }
        }

        public ClipboardFileDataViewModel(ClipboardFileData data) : base(data)
        {
        }
    }
}

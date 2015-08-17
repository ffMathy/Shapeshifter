using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard
{
    /// <summary>
    /// Interaction logic for ClipboardFileDataControl.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ClipboardFileDataControl : UserControl, IClipboardFileDataControl
    {
        public ClipboardFileDataControl()
        {
            InitializeComponent();
        }
    }
}

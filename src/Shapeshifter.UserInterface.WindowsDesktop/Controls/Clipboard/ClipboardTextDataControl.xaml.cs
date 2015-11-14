using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard
{
    /// <summary>
    ///     Interaction logic for ClipboardTextDataControl.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ClipboardTextDataControl : UserControl, IClipboardTextDataControl
    {
        public ClipboardTextDataControl()
        {
            InitializeComponent();
        }
    }
}
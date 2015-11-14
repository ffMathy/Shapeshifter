using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard
{
    /// <summary>
    ///     Interaction logic for ClipboardImageDataControl.xaml
    /// </summary>
    public partial class ClipboardImageDataControl : UserControl, IClipboardImageDataControl
    {
        [ExcludeFromCodeCoverage]
        public ClipboardImageDataControl()
        {
            InitializeComponent();
        }
    }
}
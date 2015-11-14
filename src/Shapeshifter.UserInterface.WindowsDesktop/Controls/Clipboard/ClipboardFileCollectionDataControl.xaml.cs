using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard
{
    /// <summary>
    ///     Interaction logic for ClipboardFileCollectionDataControl.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ClipboardFileCollectionDataControl : UserControl, IClipboardFileCollectionDataControl
    {
        public ClipboardFileCollectionDataControl()
        {
            InitializeComponent();
        }
    }
}
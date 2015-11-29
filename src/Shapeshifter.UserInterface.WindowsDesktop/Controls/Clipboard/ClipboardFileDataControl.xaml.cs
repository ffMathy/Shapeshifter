namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Controls;

    /// <summary>
    ///     Interaction logic for ClipboardFileDataControl.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ClipboardFileDataControl
        : UserControl,
          IClipboardFileDataControl
    {
        public ClipboardFileDataControl()
        {
            InitializeComponent();
        }
    }
}
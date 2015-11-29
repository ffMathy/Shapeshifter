namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Controls;

    /// <summary>
    ///     Interaction logic for ClipboardTextDataControl.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ClipboardTextDataControl
        : UserControl,
          IClipboardTextDataControl
    {
        public ClipboardTextDataControl()
        {
            InitializeComponent();
        }
    }
}
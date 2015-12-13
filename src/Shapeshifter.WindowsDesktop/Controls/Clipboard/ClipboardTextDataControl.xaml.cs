namespace Shapeshifter.WindowsDesktop.Controls.Clipboard
{
    using System.Windows.Controls;

    using Interfaces;

    /// <summary>
    ///     Interaction logic for ClipboardTextDataControl.xaml
    /// </summary>
    
    public partial class ClipboardTextDataControl
        : UserControl,
          IClipboardControl
    {
        public ClipboardTextDataControl()
        {
            InitializeComponent();
        }
    }
}
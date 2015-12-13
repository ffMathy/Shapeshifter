namespace Shapeshifter.WindowsDesktop.Controls.Clipboard
{
    using System.Windows.Controls;

    using Interfaces;

    /// <summary>
    ///     Interaction logic for ClipboardFileDataControl.xaml
    /// </summary>
    
    public partial class ClipboardFileDataControl
        : UserControl,
          IClipboardControl
    {
        public ClipboardFileDataControl()
        {
            InitializeComponent();
        }
    }
}
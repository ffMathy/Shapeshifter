namespace Shapeshifter.WindowsDesktop.Controls.Clipboard
{
    using System.Windows.Controls;

    using Interfaces;

    /// <summary>
    ///     Interaction logic for ClipboardFileCollectionDataControl.xaml
    /// </summary>
    public partial class ClipboardFileCollectionDataControl
        : UserControl,
          IClipboardControl
    {
        public ClipboardFileCollectionDataControl()
        {
            InitializeComponent();
        }
    }
}
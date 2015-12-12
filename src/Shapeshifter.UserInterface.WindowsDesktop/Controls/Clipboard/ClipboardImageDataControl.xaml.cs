namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard
{
    using System.Windows.Controls;

    using Interfaces;

    /// <summary>
    ///     Interaction logic for ClipboardImageDataControl.xaml
    /// </summary>
    public partial class ClipboardImageDataControl
        : UserControl,
          IClipboardControl
    {
        
        public ClipboardImageDataControl()
        {
            InitializeComponent();
        }
    }
}
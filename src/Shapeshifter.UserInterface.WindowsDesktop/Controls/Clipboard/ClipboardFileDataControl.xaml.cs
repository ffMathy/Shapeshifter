namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard
{
    using System.Diagnostics.CodeAnalysis;
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
namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard
{
    using System.Diagnostics.CodeAnalysis;
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
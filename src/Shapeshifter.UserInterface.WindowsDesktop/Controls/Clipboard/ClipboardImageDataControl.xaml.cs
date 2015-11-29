namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Controls;

    using Interfaces;

    /// <summary>
    ///     Interaction logic for ClipboardImageDataControl.xaml
    /// </summary>
    public partial class ClipboardImageDataControl
        : UserControl,
          IClipboardImageDataControl
    {
        [ExcludeFromCodeCoverage]
        public ClipboardImageDataControl()
        {
            InitializeComponent();
        }
    }
}
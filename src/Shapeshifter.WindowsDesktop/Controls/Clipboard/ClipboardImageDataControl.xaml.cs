namespace Shapeshifter.WindowsDesktop.Controls.Clipboard
{
    using System.Windows.Controls;

    using Interfaces;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    ///     Interaction logic for ClipboardImageDataControl.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ClipboardImageDataControl
        : UserControl,
          IClipboardControl
    {
        public ClipboardImageDataControl()
        {
            InitializeComponent();
		}

		public IClipboardControl Clone()
		{
			return new ClipboardImageDataControl() {
				DataContext = DataContext
			};
		}
	}
}
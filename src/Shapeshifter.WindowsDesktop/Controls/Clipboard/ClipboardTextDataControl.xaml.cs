namespace Shapeshifter.WindowsDesktop.Controls.Clipboard
{
    using System.Windows.Controls;

    using Interfaces;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    ///     Interaction logic for ClipboardTextDataControl.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ClipboardTextDataControl
        : UserControl,
          IClipboardControl
    {
        public ClipboardTextDataControl()
        {
            InitializeComponent();
		}

		public IClipboardControl Clone()
		{
			return new ClipboardTextDataControl() {
				DataContext = DataContext
			};
		}
	}
}
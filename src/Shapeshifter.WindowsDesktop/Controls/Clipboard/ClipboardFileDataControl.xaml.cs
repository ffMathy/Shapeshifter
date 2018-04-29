namespace Shapeshifter.WindowsDesktop.Controls.Clipboard
{
    using System.Windows.Controls;

    using Interfaces;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    ///     Interaction logic for ClipboardFileDataControl.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ClipboardFileDataControl
        : UserControl,
          IClipboardControl
    {
        public ClipboardFileDataControl()
        {
            InitializeComponent();
		}

		public IClipboardControl Clone()
		{
			return new ClipboardFileDataControl() {
				DataContext = DataContext
			};
		}
	}
}
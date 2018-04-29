namespace Shapeshifter.WindowsDesktop.Controls.Clipboard
{
	using System.Windows.Controls;

	using Interfaces;
	using System.Diagnostics.CodeAnalysis;

	/// <summary>
	///     Interaction logic for ClipboardFileCollectionDataControl.xaml
	/// </summary>
	[ExcludeFromCodeCoverage]
	public partial class ClipboardFileCollectionDataControl
		: UserControl,
		  IClipboardControl
	{
		public ClipboardFileCollectionDataControl()
		{
			InitializeComponent();
		}

		public IClipboardControl Clone()
		{
			return new ClipboardFileCollectionDataControl() {
				DataContext = DataContext
			};
		}
	}
}
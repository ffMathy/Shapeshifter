
namespace Shapeshifter.WindowsDesktop.Controls.Window
{
	using Shapeshifter.WindowsDesktop.Controls.Window.Interfaces;
	using Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Documents;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using System.Windows.Shapes;

	/// <summary>
	/// Interaction logic for SourceClipboardQuantityOverlayWindow.xaml
	/// </summary>
	public partial class SourceClipboardQuantityOverlay : Window, ISourceClipboardQuantityOverlay
	{
		private readonly ISourceClipboardQuantityOverlayViewModel viewModel;

		public SourceClipboardQuantityOverlay(
			ISourceClipboardQuantityOverlayViewModel viewModel)
		{
			InitializeComponent();

			DataContext = this.viewModel = viewModel;

			SetupEvents();
		}

		void SourceClipboardQuantityOverlayWindow_Activated(object sender, EventArgs e)
		{
			Activated -= SourceClipboardQuantityOverlayWindow_Activated;
			Hide();
		}

		void SetupEvents()
		{
			viewModel.ClipboardQuantityShown += ViewModel_ClipboardQuantityShown;
			viewModel.ClipboardQuantityHidden += ViewModel_ClipboardQuantityHidden;
		}

		void ViewModel_ClipboardQuantityHidden(object sender, Infrastructure.Events.DataSourceClipboardQuantityHiddenEventArgument e)
		{
			Hide();
		}

		void ViewModel_ClipboardQuantityShown(object sender, Infrastructure.Events.DataSourceClipboardQuantityShownEventArgument e)
		{
			Left = viewModel.ActiveScreen.WorkingArea.X;
			Top = viewModel.ActiveScreen.WorkingArea.Y;
			Width = viewModel.ActiveScreen.WorkingArea.Width;
			Height = viewModel.ActiveScreen.WorkingArea.Height;

			Show();
		}

		public void Dispose()
		{
			viewModel.ClipboardQuantityHidden -= ViewModel_ClipboardQuantityHidden;
			viewModel.ClipboardQuantityShown -= ViewModel_ClipboardQuantityShown;
		}
	}
}

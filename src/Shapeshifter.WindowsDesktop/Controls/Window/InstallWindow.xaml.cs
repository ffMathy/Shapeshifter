namespace Shapeshifter.WindowsDesktop.Controls.Window
{
	using Interfaces;

	using ViewModels.Interfaces;

	using Window = System.Windows.Window;

	/// <summary>
	/// Interaction logic for InstallWindow.xaml
	/// </summary>
	public partial class InstallWindow : Window, IInstallWindow
	{
		public InstallWindow(
			IInstallViewModel installViewModel)
		{
			InitializeComponent();
			DataContext = installViewModel;
		}
	}
}

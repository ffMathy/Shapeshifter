namespace Shapeshifter.WindowsDesktop.Controls.Window
{
	using Interfaces;

	using Window = System.Windows.Window;

	/// <summary>
	/// Interaction logic for MaintenanceWindow.xaml
	/// </summary>
	public partial class MaintenanceWindow : Window, IMaintenanceWindow
	{
		public MaintenanceWindow()
		{
			InitializeComponent();
		}

		public void Show(string progressText)
		{
			ProgressText.Content = progressText;
			Show();
		}
	}
}

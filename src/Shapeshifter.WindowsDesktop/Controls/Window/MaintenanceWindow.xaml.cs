namespace Shapeshifter.WindowsDesktop.Controls.Window
{
	using System.ComponentModel;

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

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			e.Cancel = true;
		}

		public void Show(string progressText)
		{
			lock (this)
			{
				ProgressText.Content = progressText;
				Show();
			}
		}

		void IMaintenanceWindow.Hide()
		{
			lock (this)
			{
				Hide();
			}
		}
	}
}

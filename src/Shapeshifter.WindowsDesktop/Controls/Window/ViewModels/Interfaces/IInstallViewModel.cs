namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces
{
	using System.ComponentModel;
	using System.Windows.Input;

	public interface IInstallViewModel: INotifyPropertyChanged
	{
		string InstallDirectory { get; }

		ICommand InstallCommand { get; }
	}
}
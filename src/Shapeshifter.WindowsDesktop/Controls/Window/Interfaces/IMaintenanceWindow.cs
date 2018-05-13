namespace Shapeshifter.WindowsDesktop.Controls.Window.Interfaces
{
	using Infrastructure.Dependencies.Interfaces;

	public interface IMaintenanceWindow: ISingleInstance
	{
		void Show(string progressText);
		void Hide();
	}
}
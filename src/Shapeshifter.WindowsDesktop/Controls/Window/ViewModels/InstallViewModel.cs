namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels
{
	using System;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.CompilerServices;
	using System.Windows.Input;

	using Annotations;

	using Commands;

	using Interfaces;

	using Services.Processes.Interfaces;

	class InstallViewModel: IInstallViewModel
	{
		readonly IProcessManager processManager;

		public event PropertyChangedEventHandler PropertyChanged;

		public string InstallDirectory => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
			"Shapeshifter");

		public ICommand InstallCommand { get; }

		public InstallViewModel(
			IProcessManager processManager)
		{
			this.processManager = processManager;

			InstallCommand = new RelayCommand(obj => Install());
		}

		void Install()
		{
			processManager.LaunchFileWithAdministrativeRights(
				processManager.GetCurrentProcessFilePath(),
				"install");
		}

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}

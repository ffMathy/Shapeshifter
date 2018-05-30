using Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces;
using Shapeshifter.WindowsDesktop.Data.Interfaces;
using Shapeshifter.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.WindowsDesktop.Infrastructure.Threading.Interfaces;
using Shapeshifter.WindowsDesktop.Services.Screen;
using Shapeshifter.WindowsDesktop.Services.Screen.Interfaces;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels
{
	class SourceClipboardQuantityOverlayViewModel : ISourceClipboardQuantityOverlayViewModel
	{
		readonly IScreenManager screenManager;
		readonly IThreadDeferrer threadDeferrer;
		readonly IMainViewModel mainViewModel;
		readonly ISettingsViewModel settingsViewModel;

		public event EventHandler<DataSourceClipboardQuantityShownEventArgument> ClipboardQuantityShown;
		public event EventHandler<DataSourceClipboardQuantityHiddenEventArgument> ClipboardQuantityHidden;

		public event PropertyChangedEventHandler PropertyChanged;

		ScreenInformation activeScreen;

		IDataSource source;

		int count;

		public ScreenInformation ActiveScreen
		{
			get => activeScreen;
			set
			{
				activeScreen = value;
				OnPropertyChanged();
			}
		}

		public IDataSource Source
		{
			get => source;
			set
			{
				source = value;
				OnPropertyChanged();
			}
		}

		public int Count
		{
			get => count;
			set
			{
				count = value;
				OnPropertyChanged();
			}
		}

		public SourceClipboardQuantityOverlayViewModel(
			IScreenManager screenManager,
			IThreadDeferrer threadDeferrer,
			IMainViewModel mainViewModel,
			ISettingsViewModel settingsViewModel)
		{
			this.screenManager = screenManager;
			this.threadDeferrer = threadDeferrer;
			this.mainViewModel = mainViewModel;
			this.settingsViewModel = settingsViewModel;

			SetupEvents();
		}

		void SetupEvents()
		{
			mainViewModel.UserInterfaceViewModel.UserInterfaceDataControlAdded += UserInterfaceViewModel_UserInterfaceDataControlAdded;
		}

		async void UserInterfaceViewModel_UserInterfaceDataControlAdded(object sender, UserInterfaceDataControlAddedEventArgument e)
		{
			if (settingsViewModel.IsQuietModeEnabled)
				return;

			ActiveScreen = screenManager.GetActiveScreen();
			Source = e.Package.Data.Source;
			Count = mainViewModel
				.UserInterfaceViewModel
				.Elements.Count(x => x.Data.Source.Title == Source.Title);

			ClipboardQuantityShown?.Invoke(this, new DataSourceClipboardQuantityShownEventArgument());

			await threadDeferrer.DeferAsync(1500, FireOnClipboardQuantityHidden);
		}

		void FireOnClipboardQuantityHidden()
		{
			ClipboardQuantityHidden?.Invoke(this, new DataSourceClipboardQuantityHiddenEventArgument());
		}

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void Dispose()
		{
			mainViewModel.UserInterfaceViewModel.UserInterfaceDataControlAdded -= UserInterfaceViewModel_UserInterfaceDataControlAdded;
		}
	}
}

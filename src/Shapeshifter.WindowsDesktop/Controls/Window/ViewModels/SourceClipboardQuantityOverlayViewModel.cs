using Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces;
using Shapeshifter.WindowsDesktop.Data.Interfaces;
using Shapeshifter.WindowsDesktop.Infrastructure.Environment.Interfaces;
using Shapeshifter.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.WindowsDesktop.Infrastructure.Threading.Interfaces;
using Shapeshifter.WindowsDesktop.Mediators.Interfaces;
using Shapeshifter.WindowsDesktop.Services.Clipboard.Interfaces;
using Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Interfaces;
using Shapeshifter.WindowsDesktop.Services.Screen;
using Shapeshifter.WindowsDesktop.Services.Screen.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels
{
	class SourceClipboardQuantityOverlayViewModel : ISourceClipboardQuantityOverlayViewModel
	{
		readonly IScreenManager screenManager;
		readonly IThreadDelay threadDelay;
		readonly IEnvironmentInformation environmentInformation;
		readonly IMainViewModel mainViewModel;

		public event EventHandler<DataSourceClipboardQuantityShownEventArgument> ClipboardQuantityShown;
		public event EventHandler<DataSourceClipboardQuantityHiddenEventArgument> ClipboardQuantityHidden;

		public event PropertyChangedEventHandler PropertyChanged;

		ScreenInformation activeScreen;

		IDataSource source;

		int count;

		public ScreenInformation ActiveScreen
		{
			get
			{
				return activeScreen;
			}
			set
			{
				activeScreen = value;
				OnPropertyChanged();
			}
		}

		public IDataSource Source
		{
			get
			{
				return source;
			}
			set
			{
				source = value;
				OnPropertyChanged();
			}
		}

		public int Count
		{
			get
			{
				return count;
			}
			set
			{
				count = value;
				OnPropertyChanged();
			}
		}

		public SourceClipboardQuantityOverlayViewModel(
			IScreenManager screenManager,
			IThreadDelay threadDelay,
			IEnvironmentInformation environmentInformation,
			IMainViewModel mainViewModel)
		{
			this.screenManager = screenManager;
			this.threadDelay = threadDelay;
			this.environmentInformation = environmentInformation;
			this.mainViewModel = mainViewModel;

			SetupEvents();
		}

		void SetupEvents()
		{
			mainViewModel.UserInterfaceViewModel.UserInterfaceDataControlAdded += UserInterfaceViewModel_UserInterfaceDataControlAdded;
		}

		async void UserInterfaceViewModel_UserInterfaceDataControlAdded(object sender, UserInterfaceDataControlAddedEventArgument e)
		{
			ActiveScreen = screenManager.GetActiveScreen();
			Source = e.Package.Data.Source;
			Count = mainViewModel
				.UserInterfaceViewModel
				.Elements.Count(x => x.Data.Source.Text == Source.Text);

			ClipboardQuantityShown?.Invoke(this, new DataSourceClipboardQuantityShownEventArgument());

			await threadDelay.ExecuteAsync(environmentInformation.GetIsDebugging() ? 3000 : 1500);

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

using Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces;
using Shapeshifter.WindowsDesktop.Services.Screen;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Shapeshifter.WindowsDesktop.Services.Screen.Interfaces;

namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels
{
    class MainViewModel : IMainViewModel
    {
        readonly IScreenManager screenManager;

        public event PropertyChangedEventHandler PropertyChanged;

		public IUserInterfaceViewModel UserInterfaceViewModel { get; }

		ScreenInformation activeScreen;

		public ScreenInformation ActiveScreen
        {
            get => activeScreen;
			set
            {
                activeScreen = value;
				UserInterfaceViewModel.ActiveScreen = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel(
            IScreenManager screenManager,
            IUserInterfaceViewModel userInterfaceViewModel)
        {
            this.screenManager = screenManager;

            UserInterfaceViewModel = userInterfaceViewModel;
            SetupEvents();
        }

        void SetupEvents()
        {
            UserInterfaceViewModel.UserInterfaceShown += UserInterfaceViewModel_UserInterfaceShown;
        }

        void UserInterfaceViewModel_UserInterfaceShown(object sender, Infrastructure.Events.UserInterfaceShownEventArgument e)
        {
            ActiveScreen = screenManager.GetActiveScreen();
        }
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            UserInterfaceViewModel.UserInterfaceShown -= UserInterfaceViewModel_UserInterfaceShown;
        }
    }
}

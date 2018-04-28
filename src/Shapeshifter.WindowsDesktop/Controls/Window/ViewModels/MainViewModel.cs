using Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces;
using Shapeshifter.WindowsDesktop.Services.Screen;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Shapeshifter.WindowsDesktop.Services.Screen.Interfaces;
using Shapeshifter.WindowsDesktop.Infrastructure.Events;

namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels
{
    class MainViewModel : IMainViewModel
    {
        readonly IScreenManager screenManager;

        public event PropertyChangedEventHandler PropertyChanged;

		public IUserInterfaceViewModel UserInterfaceViewModel { get; private set; }

		ScreenInformation activeScreen;

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

        void UserInterfaceViewModel_UserInterfaceShown(Object sender, Infrastructure.Events.UserInterfaceShownEventArgument e)
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

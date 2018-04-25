using Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces;
using Shapeshifter.WindowsDesktop.Services.Screen;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Shapeshifter.WindowsDesktop.Services.Screen.Interfaces;

namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels
{
    class MainViewModel : IMainViewModel
    {
        ScreenInformation activeScreen;

        readonly IScreenManager screenManager;

        public event PropertyChangedEventHandler PropertyChanged;

        public IUserInterfaceViewModel UserInterfaceViewModel { get; private set; }

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

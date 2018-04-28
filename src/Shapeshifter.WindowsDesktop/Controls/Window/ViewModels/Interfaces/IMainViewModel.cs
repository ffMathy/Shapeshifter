using Shapeshifter.WindowsDesktop.Services.Screen;
using System;
using System.ComponentModel;

namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces
{
    public interface IMainViewModel: INotifyPropertyChanged, IDisposable
    {
        ScreenInformation ActiveScreen { get; }
        IUserInterfaceViewModel UserInterfaceViewModel { get; }
    }
}
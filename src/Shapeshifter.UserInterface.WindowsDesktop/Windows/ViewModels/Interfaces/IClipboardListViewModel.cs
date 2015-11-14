#region

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Events;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels.Interfaces
{
    public interface IClipboardListViewModel : INotifyPropertyChanged
    {
        event EventHandler<UserInterfaceShownEventArgument> UserInterfaceShown;
        event EventHandler<UserInterfaceHiddenEventArgument> UserInterfaceHidden;

        IClipboardDataControlPackage SelectedElement { get; set; }
        IAction SelectedAction { get; set; }

        ObservableCollection<IClipboardDataControlPackage> Elements { get; }
        ObservableCollection<IAction> Actions { get; }
    }
}
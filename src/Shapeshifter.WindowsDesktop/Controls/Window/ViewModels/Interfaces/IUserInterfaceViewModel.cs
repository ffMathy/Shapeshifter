﻿namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;

    using Data.Interfaces;

    using Infrastructure.Events;

    public interface IUserInterfaceViewModel: INotifyPropertyChanged
    {
        event EventHandler<UserInterfaceShownEventArgument> UserInterfaceShown;
        event EventHandler<UserInterfacePaneSwappedEventArgument> UserInterfacePaneSwapped;
        event EventHandler<UserInterfaceHiddenEventArgument> UserInterfaceHidden;

        IClipboardDataControlPackage SelectedElement { get; set; }

        IActionViewModel SelectedAction { get; set; }

        ObservableCollection<IClipboardDataControlPackage> Elements { get; }

        ObservableCollection<IActionViewModel> Actions { get; }
    }
}
using Shapeshifter.Core.Actions;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels.Interfaces
{
    public interface IClipboardListViewModel : INotifyPropertyChanged
    {
        event EventHandler<UserInterfaceShownEventArgument> UserInterfaceShown;
        event EventHandler<UserInterfaceHiddenEventArgument> UserInterfaceHidden;

        IClipboardDataControlPackage SelectedElement { get; set; }
        IAction SelectedAction { get; set; }

        IList<IClipboardDataControlPackage> Elements { get; }
        IList<IAction> Actions { get; }
    }
}

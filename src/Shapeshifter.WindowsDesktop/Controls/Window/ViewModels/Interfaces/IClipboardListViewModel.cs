namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Threading.Tasks;

    using Data.Actions.Interfaces;
    using Data.Interfaces;

    using Infrastructure.Events;

    public interface IClipboardListViewModel: INotifyPropertyChanged
    {
        event EventHandler<UserInterfaceShownEventArgument> UserInterfaceShown;

        event EventHandler<UserInterfaceHiddenEventArgument> UserInterfaceHidden;

        IClipboardDataControlPackage SelectedElement { get; set; }

        IActionViewModel SelectedAction { get; set; }

        ObservableCollection<IClipboardDataControlPackage> Elements { get; }

        ObservableCollection<IActionViewModel> Actions { get; }
    }
}
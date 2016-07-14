namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces
{
    using System.ComponentModel;

    using Infrastructure.Dependencies.Interfaces;

    public interface ISettingsViewModel: INotifyPropertyChanged, ISingleInstance
    {
        bool StartWithWindows { get; set; }

        int PasteDurationBeforeUserInterfaceShowsInMilliseconds { get; set; }

        int MaximumAmountOfItemsInClipboard { get; set; }
    }
}
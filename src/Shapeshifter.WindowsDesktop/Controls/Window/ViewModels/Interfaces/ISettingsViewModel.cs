namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces
{
    using System.ComponentModel;
    using System.Windows.Input;

    using Infrastructure.Dependencies.Interfaces;

    public interface ISettingsViewModel: INotifyPropertyChanged, ISingleInstance
    {
        bool StartWithWindows { get; set; }

        int PasteDurationBeforeUserInterfaceShowsInMilliseconds { get; set; }

        int MaximumAmountOfItemsInClipboard { get; set; }

        string HotkeyString { get; }

        void OnReceiveKeyDown(Key key);
    }
}
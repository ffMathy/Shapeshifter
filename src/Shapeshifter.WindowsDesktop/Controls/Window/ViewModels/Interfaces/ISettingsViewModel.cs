namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces
{
    using System.ComponentModel;

    public interface ISettingsViewModel: INotifyPropertyChanged
    {
        bool StartWithWindows { get; set; }
    }
}
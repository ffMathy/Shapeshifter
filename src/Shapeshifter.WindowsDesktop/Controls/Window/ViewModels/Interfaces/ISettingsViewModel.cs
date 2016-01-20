using System.ComponentModel;

namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces
{
    public interface ISettingsViewModel: INotifyPropertyChanged
    {
        bool StartWithWindows { get; set; }
    }
}

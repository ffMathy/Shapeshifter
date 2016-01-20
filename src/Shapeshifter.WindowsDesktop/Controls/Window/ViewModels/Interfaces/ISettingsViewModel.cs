using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces
{
    public interface ISettingsViewModel: INotifyPropertyChanged
    {
        bool StartWithWindows { get; set; }
    }
}

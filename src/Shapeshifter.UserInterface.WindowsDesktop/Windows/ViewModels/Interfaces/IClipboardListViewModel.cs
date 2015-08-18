using Shapeshifter.Core.Actions;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels.Interfaces
{
    public interface IClipboardListViewModel : INotifyPropertyChanged
    {
        IClipboardControlDataPackage SelectedElement { get; set; }
        IAction SelectedAction { get; set; }

        IList<IClipboardControlDataPackage> Elements { get; }
        IList<IAction> Actions { get; }
    }
}

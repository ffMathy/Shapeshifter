using System;
using System.Collections.ObjectModel;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Data;
using System.ComponentModel;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels
{
    class ClipboardListViewModel : INotifyPropertyChanged
    {
        private IClipboardControlDataPackage selectedElement;

        public ObservableCollection<IClipboardControlDataPackage> Elements { get; private set; }
        public ObservableCollection<IAction<IClipboardData>> Actions { get; private set; }

        public IClipboardControlDataPackage SelectedElement
        {
            get
            {
                return selectedElement;
            }
            set
            {
                selectedElement = value;
                if(PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(SelectedElement)));
                }

                if(selectedElement != null)
                {
                    SetActions();
                }
            }
        }

        private void SetActions()
        {
            Actions.Clear();
        }

        public IAction<IClipboardData> SelectedAction { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ClipboardListViewModel(IClipboardUserInterfaceMediator service)
        {
            Elements = new ObservableCollection<IClipboardControlDataPackage>();
            Actions = new ObservableCollection<IAction<IClipboardData>>();

            service.ControlAdded += Service_ControlAdded;
            service.ControlHighlighted += Service_ControlHighlighted;
            service.ControlPinned += Service_ControlPinned;
            service.ControlRemoved += Service_ControlRemoved;
        }

        private void Service_ControlRemoved(object sender, Services.Events.ControlEventArgument e)
        {
            Elements.Remove(e.Package);
        }

        private void Service_ControlPinned(object sender, Services.Events.ControlEventArgument e)
        {
            throw new NotImplementedException();
        }

        private void Service_ControlHighlighted(object sender, Services.Events.ControlEventArgument e)
        {
            Elements.Remove(e.Package);
            Elements.Insert(0, e.Package);
        }

        private void Service_ControlAdded(object sender, Services.Events.ControlEventArgument e)
        {
            Elements.Insert(0, e.Package);
        }
    }
}

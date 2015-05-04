using System;
using System.Collections.ObjectModel;
using System.Windows;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels
{
    class ClipboardListViewModel
    {
        public ObservableCollection<UIElement> Elements { get; private set; }

        public ClipboardListViewModel(IClipboardUserInterfaceManagementService service)
        {
            Elements = new ObservableCollection<UIElement>();

            service.ControlAdded += Service_ControlAdded;
            service.ControlHighlighted += Service_ControlHighlighted;
            service.ControlPinned += Service_ControlPinned;
            service.ControlRemoved += Service_ControlRemoved;
        }

        private void Service_ControlRemoved(object sender, Services.Events.ControlEventArgument e)
        {
            Elements.Remove(e.Control);
        }

        private void Service_ControlPinned(object sender, Services.Events.ControlEventArgument e)
        {
            throw new NotImplementedException();
        }

        private void Service_ControlHighlighted(object sender, Services.Events.ControlEventArgument e)
        {
            Elements.Remove(e.Control);
            Elements.Insert(0, e.Control);
        }

        private void Service_ControlAdded(object sender, Services.Events.ControlEventArgument e)
        {
            Elements.Insert(0, e.Control);
        }
    }
}

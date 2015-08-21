using System.Collections.ObjectModel;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.Core.Actions;
using System.ComponentModel;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels.Interfaces;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using System.Threading.Tasks;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels
{
    class ClipboardListViewModel : 
        IClipboardListViewModel
    {
        private IClipboardDataControlPackage selectedElement;
        private IAction selectedAction;

        private readonly IEnumerable<IAction> allActions;

        public event EventHandler<UserInterfaceShownEventArgument> UserInterfaceShown;
        public event EventHandler<UserInterfaceHiddenEventArgument> UserInterfaceHidden;

        public event PropertyChangedEventHandler PropertyChanged;

        public IList<IClipboardDataControlPackage> Elements { get; private set; }
        public IList<IAction> Actions { get; private set; }

        public IAction SelectedAction
        {
            get
            {
                return selectedAction;
            }
            set
            {
                selectedAction = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(SelectedAction)));
                }
            }
        }

        public IClipboardDataControlPackage SelectedElement
        {
            get
            {
                return selectedElement;
            }
            set
            {
                selectedElement = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(SelectedElement)));
                }

                SetActions();
            }
        }

        public ClipboardListViewModel(
            IEnumerable<IAction> allActions,
            IClipboardUserInterfaceMediator mediator)
        {
            Elements = new ObservableCollection<IClipboardDataControlPackage>();
            Actions = new ObservableCollection<IAction>();

            this.allActions = allActions;

            RegisterMediatorEvents(mediator);
        }

        private void RegisterMediatorEvents(IClipboardUserInterfaceMediator mediator)
        {
            mediator.ControlAdded += Service_ControlAdded;
            mediator.ControlHighlighted += Service_ControlHighlighted;
            mediator.ControlRemoved += Service_ControlRemoved;

            mediator.UserInterfaceHidden += Service_UserInterfaceHidden;
            mediator.UserInterfaceShown += Service_UserInterfaceShown;
        }

        private void Service_UserInterfaceShown(object sender, UserInterfaceShownEventArgument e)
        {
            if(UserInterfaceShown != null)
            {
                UserInterfaceShown(this, e);
            }
        }

        private void Service_UserInterfaceHidden(object sender, UserInterfaceHiddenEventArgument e)
        {
            if (UserInterfaceHidden != null)
            {
                UserInterfaceHidden(this, e);
            }
        }

        private async void SetActions()
        {
            Actions.Clear();
            SelectedAction = null;

            if (selectedElement != null)
            {
                foreach (var data in selectedElement.Contents)
                {
                    await AddActionsFromDataAsync(data);
                }
            }
        }

        private async Task AddActionsFromDataAsync(IClipboardData data)
        {
            foreach (var action in allActions)
            {
                if (await action.CanPerformAsync(data))
                {
                    AddAction(action);
                }
            }
        }

        private void AddAction(IAction action)
        {
            Actions.Add(action);
            if (SelectedAction == null)
            {
                SelectedAction = action;
            }
        }

        private void Service_ControlRemoved(object sender, Services.Events.ControlEventArgument e)
        {
            lock(Elements)
            {
                Elements.Remove(e.Package);
            }
        }

        private void Service_ControlHighlighted(object sender, Services.Events.ControlEventArgument e)
        {
            lock(Elements)
            {
                Elements.Remove(e.Package);
                Elements.Insert(0, e.Package);
            }
        }

        private void Service_ControlAdded(object sender, Services.Events.ControlEventArgument e)
        {
            lock(Elements)
            {
                Elements.Insert(0, e.Package);
                SelectedElement = e.Package;
            }
        }
    }
}

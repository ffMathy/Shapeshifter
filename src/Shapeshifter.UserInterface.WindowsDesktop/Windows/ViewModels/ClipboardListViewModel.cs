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
using System.Linq;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels
{
    class ClipboardListViewModel :
        IClipboardListViewModel
    {
        IClipboardDataControlPackage selectedElement;
        IAction selectedAction;

        readonly IAction[] allActions;

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
            IAction[] allActions,
            IClipboardUserInterfaceMediator mediator)
        {
            Elements = new ObservableCollection<IClipboardDataControlPackage>();
            Actions = new ObservableCollection<IAction>();

            this.allActions = allActions;

            RegisterMediatorEvents(mediator);
        }

        void RegisterMediatorEvents(IClipboardUserInterfaceMediator mediator)
        {
            mediator.ControlAdded += Service_ControlAdded;
            mediator.ControlHighlighted += Service_ControlHighlighted;
            mediator.ControlRemoved += Service_ControlRemoved;

            mediator.UserInterfaceHidden += Service_UserInterfaceHidden;
            mediator.UserInterfaceShown += Service_UserInterfaceShown;
        }

        void Service_UserInterfaceShown(object sender, UserInterfaceShownEventArgument e)
        {
            if (UserInterfaceShown != null)
            {
                UserInterfaceShown(this, e);
            }
        }

        async void Service_UserInterfaceHidden(object sender, UserInterfaceHiddenEventArgument e)
        {
            if (UserInterfaceHidden != null)
            {
                UserInterfaceHidden(this, e);
            }

            await InvokeSelectedActionOnSelectedClipboardData();
        }

        async Task InvokeSelectedActionOnSelectedClipboardData()
        {
            foreach (var clipboardData in SelectedElement.Contents)
            {
                if (await SelectedAction.CanPerformAsync(clipboardData))
                {
                    await SelectedAction.PerformAsync(
                        clipboardData);
                }
            }
        }

        async void SetActions()
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

        async Task AddActionsFromDataAsync(IClipboardData data)
        {
            var actionPerformResults = await Task.WhenAll(allActions.Select(x => x.CanPerformAsync(data)));

            var allowedActions = GetAllowedActionsFromPerformResults(actionPerformResults);
            AddActions(allowedActions);
        }

        IEnumerable<IAction> GetAllowedActionsFromPerformResults(bool[] actionPerformResults)
        {
            var allowedActions = new List<IAction>();
            for (var i = 0; i < allActions.Length; i++)
            {
                if (actionPerformResults[i])
                {
                    allowedActions.Add(allActions[i]);
                }
            }

            return allowedActions;
        }

        void AddActions(IEnumerable<IAction> actions)
        {
            foreach (var action in actions.OrderBy(x => x.Order))
            {
                AddAction(action);
            }
        }

        void AddAction(IAction action)
        {
            Actions.Add(action);
            if (SelectedAction == null)
            {
                SelectedAction = action;
            }
        }

        void Service_ControlRemoved(object sender, ControlEventArgument e)
        {
            lock (Elements)
            {
                Elements.Remove(e.Package);
            }
        }

        void Service_ControlHighlighted(object sender, ControlEventArgument e)
        {
            lock (Elements)
            {
                Elements.Remove(e.Package);
                Elements.Insert(0, e.Package);
            }
        }

        void Service_ControlAdded(object sender, ControlEventArgument e)
        {
            lock (Elements)
            {
                Elements.Insert(0, e.Package);
                SelectedElement = e.Package;
            }
        }
    }
}

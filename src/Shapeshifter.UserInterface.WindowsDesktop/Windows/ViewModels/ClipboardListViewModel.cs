#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Mediators.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Binders.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels
{
    internal class ClipboardListViewModel :
        IClipboardListViewModel
    {
        private IClipboardDataControlPackage selectedElement;
        private IAction selectedAction;

        private readonly IAction[] allActions;
        private readonly IAsyncListDictionaryBinder<IClipboardDataControlPackage, IAction> packageActionBinder;
        private readonly IAsyncFilter asyncFilter;
        private readonly IPerformanceHandleFactory performanceHandleFactory;
        private readonly IUserInterfaceThread userInterfaceThread;

        private bool isFocusInActionsList;

        public event EventHandler<UserInterfaceShownEventArgument> UserInterfaceShown;
        public event EventHandler<UserInterfaceHiddenEventArgument> UserInterfaceHidden;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<IClipboardDataControlPackage> Elements { get; }
        public ObservableCollection<IAction> Actions { get; }

        public IAction SelectedAction
        {
            get { return selectedAction; }
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
            get { return selectedElement; }
            set
            {
                selectedElement = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(SelectedElement)));
                }

                userInterfaceThread.Invoke(() => packageActionBinder.LoadFromKey(value));
            }
        }

        public ClipboardListViewModel(
            IAction[] allActions,
            IClipboardUserInterfaceMediator clipboardUserInterfaceMediator,
            IKeyInterceptor hotkeyInterceptor,
            IAsyncListDictionaryBinder<IClipboardDataControlPackage, IAction> packageActionBinder,
            IAsyncFilter asyncFilter,
            IPerformanceHandleFactory performanceHandleFactory,
            IUserInterfaceThread userInterfaceThread)
        {
            Elements = new ObservableCollection<IClipboardDataControlPackage>();
            Actions = new ObservableCollection<IAction>();

            Actions.CollectionChanged += Actions_CollectionChanged;

            var pasteAction = allActions.OfType<IPasteAction>().Single();

            this.allActions = allActions.Where(x => x != pasteAction).ToArray();
            this.packageActionBinder = packageActionBinder;
            this.asyncFilter = asyncFilter;
            this.performanceHandleFactory = performanceHandleFactory;
            this.userInterfaceThread = userInterfaceThread;

            PreparePackageBinder(packageActionBinder, pasteAction);

            RegisterMediatorEvents(clipboardUserInterfaceMediator);
            RegisterKeyEvents(hotkeyInterceptor);
        }

        private void PreparePackageBinder(
            IAsyncListDictionaryBinder<IClipboardDataControlPackage, IAction> packageActionBinder,
            IPasteAction defaultAction)
        {
            packageActionBinder.Default = defaultAction;
            packageActionBinder.Bind(Elements, Actions, GetSupportedActionsFromDataAsync);
        }

        private void Actions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (SelectedAction == null && Actions.Count > 0)
            {
                SelectedAction = Actions.First();
            }
        }

        private void RegisterKeyEvents(
            IKeyInterceptor hotkeyInterceptor)
        {
            hotkeyInterceptor.HotkeyFired += HotkeyInterceptor_HotkeyFired;
        }

        private void RegisterMediatorEvents(
            IClipboardUserInterfaceMediator mediator)
        {
            mediator.ControlAdded += Service_ControlAdded;
            mediator.ControlHighlighted += Service_ControlHighlighted;
            mediator.ControlRemoved += Service_ControlRemoved;

            mediator.UserInterfaceHidden += Service_UserInterfaceHidden;
            mediator.UserInterfaceShown += Service_UserInterfaceShown;
        }

        private void HotkeyInterceptor_HotkeyFired(object sender, HotkeyFiredArgument e)
        {
            switch (e.KeyCode)
            {
                case KeyboardApi.VK_KEY_DOWN:
                    HandleDownPressed();
                    break;

                case KeyboardApi.VK_KEY_UP:
                    HandleUpPressed();
                    break;

                case KeyboardApi.VK_KEY_LEFT:
                    HandleLeftPressed();
                    break;

                case KeyboardApi.VK_KEY_RIGHT:
                    HandleRightPressed();
                    break;
            }
        }

        private void HandleRightPressed()
        {
            isFocusInActionsList = true;
        }

        private void HandleLeftPressed()
        {
            isFocusInActionsList = false;
        }

        private void HandleUpPressed()
        {
            if (isFocusInActionsList)
            {
                SelectedAction = GetNewSelectedElementAfterHandlingUpKey(Actions, SelectedAction);
            }
            else
            {
                SelectedElement = GetNewSelectedElementAfterHandlingUpKey(Elements, SelectedElement);
            }
        }

        private void HandleDownPressed()
        {
            if (isFocusInActionsList)
            {
                SelectedAction = GetNewSelectedElementAfterHandlingDownKey(Actions, SelectedAction);
            }
            else
            {
                SelectedElement = GetNewSelectedElementAfterHandlingDownKey(Elements, SelectedElement);
            }
        }

        private T GetNewSelectedElementAfterHandlingUpKey<T>(IList<T> list, T selectedElement)
        {
            var indexToUse = list.IndexOf(selectedElement) - 1;
            if (indexToUse < 0)
            {
                indexToUse = list.Count - 1;
            }

            return list[indexToUse];
        }

        private T GetNewSelectedElementAfterHandlingDownKey<T>(IList<T> list, T selectedElement)
        {
            var indexToUse = list.IndexOf(selectedElement) + 1;
            if (indexToUse == list.Count)
            {
                indexToUse = 0;
            }

            return list[indexToUse];
        }

        private async void Service_UserInterfaceShown(object sender, UserInterfaceShownEventArgument e)
        {
            if (UserInterfaceShown != null)
            {
                UserInterfaceShown(this, e);
            }
        }

        private async void Service_UserInterfaceHidden(object sender, UserInterfaceHiddenEventArgument e)
        {
            if (UserInterfaceHidden != null)
            {
                UserInterfaceHidden(this, e);
            }

            if (SelectedAction != null)
            {
                await SelectedAction.PerformAsync(SelectedElement);
            }
        }

        private async Task<IEnumerable<IAction>> GetSupportedActionsFromDataAsync(IClipboardDataPackage data)
        {
            using (performanceHandleFactory.StartMeasuringPerformance())
            {
                var allowedActions =
                    await
                        asyncFilter.FilterAsync(allActions, action => action.CanPerformAsync(data))
                            .ConfigureAwait(false);
                return allowedActions.OrderBy(x => x.Order);
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

        private void Service_ControlRemoved(object sender, ControlEventArgument e)
        {
            lock (Elements)
            {
                Elements.Remove(e.Package);
            }
        }

        private void Service_ControlHighlighted(object sender, ControlEventArgument e)
        {
            lock (Elements)
            {
                Elements.Remove(e.Package);
                Elements.Insert(0, e.Package);
            }
        }

        private void Service_ControlAdded(object sender, ControlEventArgument e)
        {
            lock (Elements)
            {
                userInterfaceThread.Invoke(() => Elements.Insert(0, e.Package));
                SelectedElement = e.Package;
            }
        }
    }
}
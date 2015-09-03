using System.Collections.ObjectModel;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.Core.Actions;
using System.ComponentModel;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using System.Threading.Tasks;
using System;
using System.Linq;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels
{
    class ClipboardListViewModel :
        IClipboardListViewModel
    {
        IClipboardDataControlPackage selectedElement;
        IAction selectedAction;

        readonly IAction[] allActions;
        readonly IAsyncListDictionaryBinder<IClipboardDataControlPackage, IAction> packageActionBinder;
        readonly IAsyncFilter asyncFilter;
        readonly IPerformanceHandleFactory performanceHandleFactory;

        bool isFocusInActionsList;

        public event EventHandler<UserInterfaceShownEventArgument> UserInterfaceShown;
        public event EventHandler<UserInterfaceHiddenEventArgument> UserInterfaceHidden;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<IClipboardDataControlPackage> Elements { get; private set; }
        public ObservableCollection<IAction> Actions { get; private set; }

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
                
                packageActionBinder.LoadFromKey(value);
            }
        }

        public ClipboardListViewModel(
            IAction[] allActions,
            IClipboardUserInterfaceMediator clipboardUserInterfaceMediator,
            IKeyInterceptor hotkeyInterceptor,
            IAsyncListDictionaryBinder<IClipboardDataControlPackage, IAction> packageActionBinder,
            IAsyncFilter asyncFilter,
            IPerformanceHandleFactory performanceHandleFactory)
        {
            Elements = new ObservableCollection<IClipboardDataControlPackage>();
            Actions = new ObservableCollection<IAction>();

            Actions.CollectionChanged += Actions_CollectionChanged;

            var pasteAction = allActions.OfType<IPasteAction>().Single();

            this.allActions = allActions.Where(x => x != pasteAction).ToArray();
            this.packageActionBinder = packageActionBinder;
            this.asyncFilter = asyncFilter;
            this.performanceHandleFactory = performanceHandleFactory;

            PreparePackageBinder(packageActionBinder, pasteAction);

            RegisterMediatorEvents(clipboardUserInterfaceMediator);
            RegisterKeyEvents(hotkeyInterceptor);
        }

        private void PreparePackageBinder(IAsyncListDictionaryBinder<IClipboardDataControlPackage, IAction> packageActionBinder, IPasteAction defaultAction)
        {
            packageActionBinder.Default = defaultAction;
            packageActionBinder.Bind(Elements, Actions, GetSupportedActionsFromDataAsync);
        }

        void Actions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(SelectedAction == null && Actions.Count > 0)
            {
                SelectedAction = Actions.First();
            }
        }

        void RegisterKeyEvents(
            IKeyInterceptor hotkeyInterceptor)
        {
            hotkeyInterceptor.HotkeyFired += HotkeyInterceptor_HotkeyFired;
        }

        void RegisterMediatorEvents(
            IClipboardUserInterfaceMediator mediator)
        {
            mediator.ControlAdded += Service_ControlAdded;
            mediator.ControlHighlighted += Service_ControlHighlighted;
            mediator.ControlRemoved += Service_ControlRemoved;

            mediator.UserInterfaceHidden += Service_UserInterfaceHidden;
            mediator.UserInterfaceShown += Service_UserInterfaceShown;
        }

        void HotkeyInterceptor_HotkeyFired(object sender, HotkeyFiredArgument e)
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

        void HandleRightPressed()
        {
            isFocusInActionsList = true;
        }

        void HandleLeftPressed()
        {
            isFocusInActionsList = false;
        }

        void HandleUpPressed()
        {
            if(isFocusInActionsList)
            {
                SelectedAction = GetNewSelectedElementAfterHandlingUpKey(Actions, SelectedAction);
            } else
            {
                SelectedElement = GetNewSelectedElementAfterHandlingUpKey(Elements, SelectedElement);
            }
        }

        void HandleDownPressed()
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

        T GetNewSelectedElementAfterHandlingUpKey<T>(IList<T> list, T selectedElement)
        {
            var indexToUse = list.IndexOf(selectedElement) - 1;
            if (indexToUse < 0)
            {
                indexToUse = list.Count - 1;
            }

            return list[indexToUse];
        }

        T GetNewSelectedElementAfterHandlingDownKey<T>(IList<T> list, T selectedElement)
        {
            var indexToUse = list.IndexOf(selectedElement) + 1;
            if (indexToUse == list.Count)
            {
                indexToUse = 0;
            }

            return list[indexToUse];
        }

        async void Service_UserInterfaceShown(object sender, UserInterfaceShownEventArgument e)
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

            if (SelectedAction != null)
            {
                await SelectedAction.PerformAsync(SelectedElement);
            }
        }

        async Task<IEnumerable<IAction>> GetSupportedActionsFromDataAsync(IClipboardDataPackage data)
        {
            using (performanceHandleFactory.StartMeasuringPerformance())
            {
                var allowedActions = await asyncFilter.FilterAsync(allActions, action => action.CanPerformAsync(data));
                return allowedActions.OrderBy(x => x.Order);
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

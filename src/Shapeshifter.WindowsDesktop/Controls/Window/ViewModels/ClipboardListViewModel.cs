namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;

    using Binders.Interfaces;

    using Data.Actions.Interfaces;
    using Data.Interfaces;

    using Infrastructure.Events;
    using Infrastructure.Handles.Factories.Interfaces;
    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Mediators.Interfaces;

    using Native;

    using Services.Messages.Interceptors.Hotkeys.Interfaces;

    class ClipboardListViewModel:
        IClipboardListViewModel
    {
        IClipboardDataControlPackage selectedElement;

        IAction selectedAction;

        readonly IAction[] allActions;

        readonly IAsyncListDictionaryBinder<IClipboardDataControlPackage, IAction>
            packageActionBinder;

        readonly IAsyncFilter asyncFilter;

        readonly IPerformanceHandleFactory performanceHandleFactory;

        readonly IUserInterfaceThread userInterfaceThread;

        bool isFocusInActionsList;

        public event EventHandler<UserInterfaceShownEventArgument> UserInterfaceShown;

        public event EventHandler<UserInterfaceHiddenEventArgument> UserInterfaceHidden;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<IClipboardDataControlPackage> Elements { get; }

        public ObservableCollection<IAction> Actions { get; }

        public IAction SelectedAction
        {
            get
            {
                return selectedAction;
            }
            set
            {
                selectedAction = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedAction)));
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedElement)));

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

            var pasteAction = allActions.OfType<IPasteAction>()
                                        .Single();

            this.allActions = allActions.Where(x => x != pasteAction)
                                        .ToArray();
            this.packageActionBinder = packageActionBinder;
            this.asyncFilter = asyncFilter;
            this.performanceHandleFactory = performanceHandleFactory;
            this.userInterfaceThread = userInterfaceThread;

            PreparePackageBinder(pasteAction);

            RegisterMediatorEvents(clipboardUserInterfaceMediator);
            RegisterKeyEvents(hotkeyInterceptor);
        }

        void PreparePackageBinder(
            IAction defaultAction)
        {
            packageActionBinder.Default = defaultAction;
            packageActionBinder.Bind(Elements, Actions, GetSupportedActionsFromDataAsync);
        }

        void Actions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if ((SelectedAction == null) && (Actions.Count > 0))
            {
                SelectedAction = Actions.First();
            }
        }

        void RegisterKeyEvents(
            IHotkeyInterceptor hotkeyInterceptor)
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
                case KeyboardNativeApi.VK_KEY_DOWN:
                    HandleDownPressed();
                    break;

                case KeyboardNativeApi.VK_KEY_UP:
                    HandleUpPressed();
                    break;

                case KeyboardNativeApi.VK_KEY_LEFT:
                    HandleRightOrLeftPressed();
                    break;

                case KeyboardNativeApi.VK_KEY_RIGHT:
                    HandleRightOrLeftPressed();
                    break;
            }
        }

        void HandleRightOrLeftPressed()
        {
            isFocusInActionsList = !isFocusInActionsList;
        }

        void HandleUpPressed()
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

        void HandleDownPressed()
        {
            if (isFocusInActionsList)
            {
                SelectedAction = GetNewSelectedElementAfterHandlingDownKey(Actions, SelectedAction);
            }
            else
            {
                SelectedElement = GetNewSelectedElementAfterHandlingDownKey(
                    Elements,
                    SelectedElement);
            }
        }

        static T GetNewSelectedElementAfterHandlingUpKey<T>(IList<T> list, T selectedElement)
        {
            var indexToUse = list.IndexOf(selectedElement) - 1;
            if (indexToUse < 0)
            {
                indexToUse = list.Count - 1;
            }

            return list[indexToUse];
        }

        static T GetNewSelectedElementAfterHandlingDownKey<T>(IList<T> list, T selectedElement)
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
            UserInterfaceShown?.Invoke(this, e);
        }

        async void Service_UserInterfaceHidden(object sender, UserInterfaceHiddenEventArgument e)
        {
            UserInterfaceHidden?.Invoke(this, e);

            if (SelectedAction != null)
            {
                await SelectedAction.PerformAsync(SelectedElement.Data);
            }
        }

        async Task<IEnumerable<IAction>> GetSupportedActionsFromDataAsync(
            IClipboardDataControlPackage data)
        {
            using (performanceHandleFactory.StartMeasuringPerformance())
            {
                var allowedActions =
                    await
                    asyncFilter.FilterAsync(allActions, action => action.CanPerformAsync(data.Data))
                               .ConfigureAwait(false);
                return allowedActions.OrderBy(x => x.Order);
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
                userInterfaceThread.Invoke(() => Elements.Insert(0, e.Package));
                SelectedElement = e.Package;
            }
        }
    }
}
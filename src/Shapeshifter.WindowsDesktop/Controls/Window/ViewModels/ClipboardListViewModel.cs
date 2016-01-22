namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Binders.Interfaces;

    using Data.Actions.Interfaces;
    using Data.Interfaces;

    using Infrastructure.Events;
    using Infrastructure.Handles.Factories.Interfaces;
    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using JetBrains.Annotations;

    using Mediators.Interfaces;

    using Services.Messages.Interceptors.Hotkeys.Interfaces;
    using Services.Screen;
    using Services.Screen.Interfaces;

    class ClipboardListViewModel:
        IClipboardListViewModel,
        INotifyPropertyChanged
    {
        IClipboardDataControlPackage selectedElement;

        IAction selectedAction;

        bool isFocusInActionsList;

        readonly IAction[] allActions;

        readonly IClipboardUserInterfaceMediator clipboardUserInterfaceMediator;
        readonly IAsyncListDictionaryBinder<IClipboardDataControlPackage, IAction> packageActionBinder;
        readonly IAsyncFilter asyncFilter;
        readonly IPerformanceHandleFactory performanceHandleFactory;
        readonly IUserInterfaceThread userInterfaceThread;
        readonly IScreenManager screenManager;
        ScreenInformation activeScreen;

        public event EventHandler<UserInterfaceShownEventArgument> UserInterfaceShown;

        public event EventHandler<UserInterfaceHiddenEventArgument> UserInterfaceHidden;

        public ObservableCollection<IClipboardDataControlPackage> Elements { get; }

        public ObservableCollection<IAction> Actions { get; }

        public ScreenInformation ActiveScreen
        {
            get
            {
                return activeScreen;
            }
            set
            {
                if (Equals(value, activeScreen))
                {
                    return;
                }
                activeScreen = value;
                OnPropertyChanged();
            }
        }

        public IAction SelectedAction
        {
            get
            {
                return selectedAction;
            }
            set
            {
                if (Equals(value, selectedAction))
                {
                    return;
                }

                selectedAction = value;
                OnPropertyChanged();
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
                if (Equals(value, selectedElement))
                {
                    return;
                }

                selectedElement = value;
                OnPropertyChanged();

                lock (Elements)
                {
                    userInterfaceThread.Invoke(() => packageActionBinder.LoadFromKey(value));
                }
            }
        }

        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
        public ClipboardListViewModel(
            IAction[] allActions,
            IClipboardUserInterfaceMediator clipboardUserInterfaceMediator,
            IKeyInterceptor hotkeyInterceptor,
            IAsyncListDictionaryBinder<IClipboardDataControlPackage, IAction> packageActionBinder,
            IAsyncFilter asyncFilter,
            IPerformanceHandleFactory performanceHandleFactory,
            IUserInterfaceThread userInterfaceThread,
            IScreenManager screenManager)
        {
            Elements = new ObservableCollection<IClipboardDataControlPackage>();
            Actions = new ObservableCollection<IAction>();

            Actions.CollectionChanged += Actions_CollectionChanged;

            var pasteAction = allActions.OfType<IPasteAction>()
                                        .Single();

            this.allActions = allActions.Where(x => x != pasteAction)
                                        .ToArray();
            this.clipboardUserInterfaceMediator = clipboardUserInterfaceMediator;
            this.packageActionBinder = packageActionBinder;
            this.asyncFilter = asyncFilter;
            this.performanceHandleFactory = performanceHandleFactory;
            this.userInterfaceThread = userInterfaceThread;
            this.screenManager = screenManager;

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
            mediator.ControlAdded += Mediator_ControlAdded;

            mediator.UserInterfaceHidden += Mediator_UserInterfaceHidden;
            mediator.UserInterfaceShown += Mediator_UserInterfaceShown;

            mediator.PastePerformed += Mediator_PastePerformed;
        }

        async void Mediator_PastePerformed(
            object sender,
            PastePerformedEventArgument e)
        {
            await PerformPaste();
        }

        async Task PerformPaste()
        {
            if (SelectedAction != null)
            {
                await SelectedAction.PerformAsync(SelectedElement.Data);
            }
        }

        void HotkeyInterceptor_HotkeyFired(object sender, HotkeyFiredArgument e)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (e.Key)
            {
                case Key.Down:
                    ShowNextItem();
                    break;

                case Key.Up:
                    ShowPreviousItem();
                    break;

                case Key.Left:
                    HandleLeftPressed();
                    break;

                case Key.Right:
                    HandleRightPressed();
                    break;
            }
        }

        public void SwapBetweenPanes()
        {
            isFocusInActionsList = !isFocusInActionsList;
        }

        void HandleLeftPressed()
        {
            if (!isFocusInActionsList)
            {
                Cancel();
            }
            else
            {
                isFocusInActionsList = !isFocusInActionsList;
            }
        }

        void HandleRightPressed()
        {
            if (isFocusInActionsList)
            {
                Cancel();
            }
            else
            {
                isFocusInActionsList = !isFocusInActionsList;
            }
        }

        void Cancel()
        {
            clipboardUserInterfaceMediator.Cancel();
        }

        public void ShowPreviousItem()
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

        public void ShowNextItem()
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

        async void Mediator_UserInterfaceShown(object sender, UserInterfaceShownEventArgument e)
        {
            ActiveScreen = screenManager.GetPrimaryScreen();

            UserInterfaceShown?.Invoke(this, e);
        }

        async void Mediator_UserInterfaceHidden(object sender, UserInterfaceHiddenEventArgument e)
        {
            HideInterface();
        }

        void HideInterface()
        {
            isFocusInActionsList = false;
            UserInterfaceHidden?.Invoke(this, new UserInterfaceHiddenEventArgument());
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

        void Mediator_ControlAdded(object sender, ControlEventArgument e)
        {
            lock (Elements)
            {
                userInterfaceThread.Invoke(() => Elements.Insert(0, e.Package));
                SelectedElement = e.Package;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
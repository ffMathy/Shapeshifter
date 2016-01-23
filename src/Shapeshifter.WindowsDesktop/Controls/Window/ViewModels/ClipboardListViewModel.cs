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

    using Binders.Interfaces;

    using Data.Actions.Interfaces;
    using Data.Interfaces;

    using Infrastructure.Events;

    using Interfaces;

    using JetBrains.Annotations;

    using Mediators.Interfaces;

    using Services.Screen;
    using Services.Screen.Interfaces;

    class ClipboardListViewModel
        :
            IClipboardListViewModel,
            IDisposable
    {
        IClipboardDataControlPackage selectedElement;
        IAction selectedAction;

        readonly IClipboardUserInterfaceInteractionMediator clipboardUserInterfaceInteractionMediator;
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
            }
        }

        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
        public ClipboardListViewModel(
            IClipboardUserInterfaceInteractionMediator clipboardUserInterfaceInteractionMediator,
            IScreenManager screenManager,
            IPackageToActionSwitch packageToActionSwitch)
        {
            Elements = new ObservableCollection<IClipboardDataControlPackage>();
            Actions = new ObservableCollection<IAction>();

            Actions.CollectionChanged += Actions_CollectionChanged;

            this.clipboardUserInterfaceInteractionMediator = clipboardUserInterfaceInteractionMediator;
            this.screenManager = screenManager;

            SetUpClipboardUserInterfaceInteractionMediator();

            packageToActionSwitch.PrepareBinder(this);
        }

        void Actions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if ((SelectedAction == null) && (Actions.Count > 0))
            {
                SelectedAction = Actions.First();
            }
        }

        void SetUpClipboardUserInterfaceInteractionMediator()
        {
            clipboardUserInterfaceInteractionMediator.PackageAdded += MediatorPackageAdded;

            clipboardUserInterfaceInteractionMediator.UserInterfaceHidden += Mediator_UserInterfaceHidden;
            clipboardUserInterfaceInteractionMediator.UserInterfaceShown += Mediator_UserInterfaceShown;

            clipboardUserInterfaceInteractionMediator.PastePerformed += Mediator_PastePerformed;

            clipboardUserInterfaceInteractionMediator.SelectedNextItem += ClipboardUserInterfaceInteractionMediator_SelectedNextItem;
            clipboardUserInterfaceInteractionMediator.SelectedPreviousItem += ClipboardUserInterfaceInteractionMediator_SelectedPreviousItem;
        }

        void ClipboardUserInterfaceInteractionMediator_SelectedPreviousItem(
            object sender,
            EventArgs e)
        {
            switch (clipboardUserInterfaceInteractionMediator.CurrentPane)
            {
                case ClipboardUserInterfacePane.Actions:
                    SelectedAction = GetNewSelectedElementAfterHandlingUpKey(Actions, SelectedAction);
                    break;

                case ClipboardUserInterfacePane.ClipboardPackages:
                    SelectedElement = GetNewSelectedElementAfterHandlingUpKey(Elements, SelectedElement);
                    break;

                default:
                    throw new InvalidOperationException(
                        "Unknown user interface pane.");
            }
        }

        void ClipboardUserInterfaceInteractionMediator_SelectedNextItem(
            object sender,
            EventArgs e)
        {
            switch (clipboardUserInterfaceInteractionMediator.CurrentPane)
            {
                case ClipboardUserInterfacePane.Actions:
                    SelectedAction = GetNewSelectedElementAfterHandlingDownKey(Actions, SelectedAction);
                    break;

                case ClipboardUserInterfacePane.ClipboardPackages:
                    SelectedElement = GetNewSelectedElementAfterHandlingDownKey(
                        Elements,
                        SelectedElement);
                    break;

                default:
                    throw new InvalidOperationException(
                        "Unknown user interface pane.");
            }
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

        static T GetNewSelectedElementAfterHandlingUpKey<T>(
            IList<T> list,
            T selectedElement)
        {
            var indexToUse = list.IndexOf(selectedElement) - 1;
            if (indexToUse < 0)
            {
                indexToUse = list.Count - 1;
            }

            return list[indexToUse];
        }

        static T GetNewSelectedElementAfterHandlingDownKey<T>(
            IList<T> list,
            T selectedElement)
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
            UserInterfaceHidden?.Invoke(
                this,
                new UserInterfaceHiddenEventArgument());
        }

        void MediatorPackageAdded(object sender, PackageEventArgument e)
        {
            lock (Elements)
            {
                Elements.Insert(0, e.Package);
                SelectedElement = e.Package;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            UnsubscribeUserInterfaceInteractionMediatorEvents();
        }

        void UnsubscribeUserInterfaceInteractionMediatorEvents()
        {
            clipboardUserInterfaceInteractionMediator.PackageAdded -= MediatorPackageAdded;

            clipboardUserInterfaceInteractionMediator.UserInterfaceHidden -= Mediator_UserInterfaceHidden;
            clipboardUserInterfaceInteractionMediator.UserInterfaceShown -= Mediator_UserInterfaceShown;

            clipboardUserInterfaceInteractionMediator.PastePerformed -= Mediator_PastePerformed;

            clipboardUserInterfaceInteractionMediator.SelectedNextItem -= ClipboardUserInterfaceInteractionMediator_SelectedNextItem;
            clipboardUserInterfaceInteractionMediator.SelectedPreviousItem -= ClipboardUserInterfaceInteractionMediator_SelectedPreviousItem;
        }
    }
}
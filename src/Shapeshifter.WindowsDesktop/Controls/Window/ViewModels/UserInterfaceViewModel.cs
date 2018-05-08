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
	using System.Threading;
	using System.Threading.Tasks;

	using Binders.Interfaces;

	using Data.Interfaces;

	using Infrastructure.Events;

	using Interfaces;

	using Mediators.Interfaces;
	using Serilog;

	using Services.Clipboard.Interfaces;
	using Services.Screen;

	class UserInterfaceViewModel : 
		IUserInterfaceViewModel, 
		IDisposable
	{
		IClipboardDataControlPackage selectedElement;
		IActionViewModel selectedAction;

		ScreenInformation activeScreen;

		readonly SemaphoreSlim singlePasteLock;

		readonly IClipboardUserInterfaceInteractionMediator clipboardUserInterfaceInteractionMediator;
		readonly ILogger logger;
		readonly IClipboardPersistanceService clipboardPersistanceService;

		public event EventHandler<UserInterfaceShownEventArgument> UserInterfaceShown;
		public event EventHandler<UserInterfaceHiddenEventArgument> UserInterfaceHidden;
		public event EventHandler<UserInterfacePaneSwappedEventArgument> UserInterfacePaneSwapped;
		public event EventHandler<UserInterfaceDataControlAddedEventArgument> UserInterfaceDataControlAdded;

		public ObservableCollection<IClipboardDataControlPackage> Elements { get; }
		public ObservableCollection<IActionViewModel> Actions { get; }

		public ScreenInformation ActiveScreen
		{
			get => activeScreen;
			set
			{
				activeScreen = value;
				OnPropertyChanged();
			}
		}

		public IActionViewModel SelectedAction
		{
			get => selectedAction;
			set
			{
				selectedAction = value;
				OnPropertyChanged();
			}
		}

		public IClipboardDataControlPackage SelectedElement
		{
			get => selectedElement;
			set
			{
				selectedElement = value;
				OnPropertyChanged();
			}
		}

		[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
		public UserInterfaceViewModel(
			IClipboardUserInterfaceInteractionMediator clipboardUserInterfaceInteractionMediator,
			IPackageToActionSwitch packageToActionSwitch,
			ILogger logger,
			IClipboardPersistanceService clipboardPersistanceService)
		{
			Elements = new ObservableCollection<IClipboardDataControlPackage>();
			Actions = new ObservableCollection<IActionViewModel>();

			singlePasteLock = new SemaphoreSlim(1);

			Actions.CollectionChanged += Actions_CollectionChanged;

			this.clipboardUserInterfaceInteractionMediator = clipboardUserInterfaceInteractionMediator;
			this.logger = logger;
			this.clipboardPersistanceService = clipboardPersistanceService;

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

			clipboardUserInterfaceInteractionMediator.PaneSwapped += ClipboardUserInterfaceInteractionMediator_PaneSwapped;

			clipboardUserInterfaceInteractionMediator.RemovedCurrentItem += ClipboardUserInterfaceInteractionMediator_RemovedCurrentItem;

			clipboardUserInterfaceInteractionMediator.UserInterfaceHidden += Mediator_UserInterfaceHidden;
			clipboardUserInterfaceInteractionMediator.UserInterfaceShown += Mediator_UserInterfaceShown;

			clipboardUserInterfaceInteractionMediator.PastePerformed += Mediator_PastePerformed;

			clipboardUserInterfaceInteractionMediator.SelectedNextItem += ClipboardUserInterfaceInteractionMediator_SelectedNextItem;
			clipboardUserInterfaceInteractionMediator.SelectedPreviousItem += ClipboardUserInterfaceInteractionMediator_SelectedPreviousItem;
		}

		void ClipboardUserInterfaceInteractionMediator_RemovedCurrentItem(object sender, EventArgs e)
		{
			RemoveCurrentElement();
		}

		void RemoveCurrentElement()
		{
			var currentElement = SelectedElement;
			var currentIndex = Elements.IndexOf(currentElement);

			Elements.Remove(currentElement);

			if (Elements.Count == 0)
			{
				HideInterface();
			}
			else
			{
				var targetIndex = currentIndex == Elements.Count - 1 ? Elements.Count - 1 : currentIndex + 1;
				SelectedElement = Elements.ElementAt(targetIndex);
			}
		}

		void ClipboardUserInterfaceInteractionMediator_PaneSwapped(object sender, EventArgs e)
		{
			var pane = clipboardUserInterfaceInteractionMediator.CurrentPane;
			OnUserInterfacePaneSwapped(new UserInterfacePaneSwappedEventArgument(pane));
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
				await singlePasteLock.WaitAsync();
				try
				{
					await SelectedAction.Action.PerformAsync(SelectedElement.Data);
					if (await clipboardPersistanceService.IsPinnedAsync(SelectedElement.Data))
						return;

					var oldSelectedElement = SelectedElement;
					SelectedElement = null;

					Elements.Remove(oldSelectedElement);

					var clone = oldSelectedElement.Clone();
					Elements.Insert(await GetIndexToInsertItemAsync(clone), clone);

					SelectedElement = clone;
				}
				finally
				{
					singlePasteLock.Release();
				}
			}
		}

		async Task<int> GetIndexToInsertItemAsync(IClipboardDataControlPackage package)
		{

		}

		static T GetNewSelectedElementAfterHandlingUpKey<T>(
			IList<T> list,
			T selectedElement)
		{
			var indexToUse = list.IndexOf(selectedElement) - 1;
			if (indexToUse < 0)
				indexToUse = list.Count - 1;

			return list[indexToUse];
		}

		static T GetNewSelectedElementAfterHandlingDownKey<T>(
			IList<T> list,
			T selectedElement)
		{
			var indexToUse = list.IndexOf(selectedElement) + 1;
			if (indexToUse == list.Count)
				indexToUse = 0;

			return list[indexToUse];
		}

		async void Mediator_UserInterfaceShown(object sender, UserInterfaceShownEventArgument e)
		{
			if (Elements.Count == 0)
			{
				logger.Information("Did not show the UI because there are no clipboard elements in the list.");
				return;
			}
			
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

		async void MediatorPackageAdded(object sender, PackageEventArgument e)
		{
			await AddElementAsync(e.Package);
		}

		async Task AddElementAsync(IClipboardDataControlPackage package)
		{
			lock (Elements)
			{
				Elements.Insert(await GetIndexToInsertItemAsync(package), package);
				SelectedElement = package;

				UserInterfaceDataControlAdded?.Invoke(this, new UserInterfaceDataControlAddedEventArgument(package));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

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

			clipboardUserInterfaceInteractionMediator.PaneSwapped -= ClipboardUserInterfaceInteractionMediator_PaneSwapped;

			clipboardUserInterfaceInteractionMediator.RemovedCurrentItem -= ClipboardUserInterfaceInteractionMediator_RemovedCurrentItem;

			clipboardUserInterfaceInteractionMediator.UserInterfaceHidden -= Mediator_UserInterfaceHidden;
			clipboardUserInterfaceInteractionMediator.UserInterfaceShown -= Mediator_UserInterfaceShown;

			clipboardUserInterfaceInteractionMediator.PastePerformed -= Mediator_PastePerformed;

			clipboardUserInterfaceInteractionMediator.SelectedNextItem -= ClipboardUserInterfaceInteractionMediator_SelectedNextItem;
			clipboardUserInterfaceInteractionMediator.SelectedPreviousItem -= ClipboardUserInterfaceInteractionMediator_SelectedPreviousItem;
		}

		protected virtual void OnUserInterfacePaneSwapped(UserInterfacePaneSwappedEventArgument e)
		{
			UserInterfacePaneSwapped?.Invoke(this, e);
		}
	}
}
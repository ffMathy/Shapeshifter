namespace Shapeshifter.WindowsDesktop.Controls.Window.Binders
{
	using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;

    using Data.Actions.Interfaces;
    using Data.Interfaces;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;
	using Serilog;
	using ViewModels;
    using ViewModels.Interfaces;

    public class SelectedElementToActionsSwitchMechanism : IPackageToActionSwitch
    {
        IAction[] allActions;
        IPasteAction pasteAction;
        IUserInterfaceViewModel viewModel;

        readonly IAsyncListDictionaryBinder<IClipboardDataControlPackage, IActionViewModel> packageActionBinder;

        readonly IAsyncFilter asyncFilter;
		readonly ILogger logger;

		public SelectedElementToActionsSwitchMechanism(
            IAction[] allActions,
            IAsyncFilter asyncFilter,
			ILogger logger,
            IAsyncListDictionaryBinder<IClipboardDataControlPackage, IActionViewModel> packageActionBinder)
        {
            this.asyncFilter = asyncFilter;
			this.logger = logger;
			this.packageActionBinder = packageActionBinder;

			PrepareActions(allActions);
        }

        async void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
			if (e.PropertyName == nameof(viewModel.SelectedElement))
				await OnSelectedItemChangedAsync();
        }

        async Task OnSelectedItemChangedAsync()
        {
            if (viewModel.SelectedElement == null)
            {
                viewModel.SelectedAction = null;
                viewModel.Actions.Clear();
                return;
            }

            packageActionBinder.Default = await GetActionViewModelFromActionAndPackageAsync(
                viewModel.SelectedElement,
                pasteAction);
            packageActionBinder.LoadFromKey(
                viewModel.SelectedElement);
        }

        static async Task<IActionViewModel> GetActionViewModelFromActionAndPackageAsync(
            IClipboardDataControlPackage package,
            IAction action)
        {
            return new ActionViewModel(
                action,
                await action.GetTitleAsync(package.Data));
        }

        void PrepareActions(IAction[] actions)
        {
            pasteAction = actions
                .OfType<IPasteAction>()
                .Single();
            allActions = actions
                .Where(x => x != pasteAction)
                .ToArray();
        }

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public void PrepareBinder(
            IUserInterfaceViewModel UserInterfaceViewModel)
        {
            viewModel = UserInterfaceViewModel;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            packageActionBinder.Bind(
                viewModel.Elements,
                viewModel.Actions,
                GetSupportedActionsFromDataAsync);
        }

        async Task<IEnumerable<IActionViewModel>> GetSupportedActionsFromDataAsync(
            IClipboardDataControlPackage data)
        {
            var allowedActions = await asyncFilter
                .FilterAsync(
                    allActions,
                    action => action.CanPerformAsync(data.Data));
            var viewModelMappingTasks = allowedActions
                .Select(x => GetActionViewModelFromActionAndPackageAsync(data, x));
            var viewModels = await Task.WhenAll(viewModelMappingTasks);
            return viewModels.OrderBy(x => x.Action.Order);
        }
    }
}
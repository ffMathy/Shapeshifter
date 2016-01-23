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

    using ViewModels.Interfaces;

    public class SelectedElementToActionsSwitchMechanism: IPackageToActionSwitch
    {
        IAction[] allActions;
        IPasteAction pasteAction;
        IClipboardListViewModel viewModel;

        readonly IAsyncListDictionaryBinder<IClipboardDataControlPackage, IAction> packageActionBinder;

        readonly IAsyncFilter asyncFilter;

        public SelectedElementToActionsSwitchMechanism(
            IAction[] allActions,
            IAsyncFilter asyncFilter,
            IAsyncListDictionaryBinder<IClipboardDataControlPackage, IAction> packageActionBinder)
        {
            this.asyncFilter = asyncFilter;
            this.packageActionBinder = packageActionBinder;

            PrepareActions(allActions);
        }

        void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(viewModel.SelectedElement))
            {
                OnSelectedItemChanged();
            }
        }

        void OnSelectedItemChanged()
        {
            lock (viewModel.Elements)
            {
                packageActionBinder.LoadFromKey(
                    viewModel.SelectedElement);
            }
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
            IClipboardListViewModel clipboardListViewModel)
        {
            viewModel = clipboardListViewModel;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            packageActionBinder.Default = pasteAction;
            packageActionBinder.Bind(
                viewModel.Elements,
                viewModel.Actions,
                GetSupportedActionsFromDataAsync);
        }

        async Task<IEnumerable<IAction>> GetSupportedActionsFromDataAsync(
            IClipboardDataControlPackage data)
        {
            var allowedActions = await asyncFilter
                                           .FilterAsync(
                                               allActions,
                                               action => action.CanPerformAsync(data.Data));
            return allowedActions
                .OrderBy(x => x.Order);
        }
    }
}
#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Binders.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.Binders
{
    internal class PackageToActionBinder : IAsyncListDictionaryBinder<IClipboardDataControlPackage, IAction>
    {
        private readonly IDictionary<IClipboardDataControlPackage, ICollection<IAction>> dictionaryStates;

        private IClipboardDataControlPackage key;
        private ObservableCollection<IAction> destinationCollection;
        private Func<IClipboardDataControlPackage, Task<IEnumerable<IAction>>> mappingFunction;

        public IAction Default { get; set; }

        public PackageToActionBinder()
        {
            dictionaryStates = new Dictionary<IClipboardDataControlPackage, ICollection<IAction>>();
        }

        //TODO: use a factory for this instead.
        public void Bind(
            ObservableCollection<IClipboardDataControlPackage> sourceCollection,
            ObservableCollection<IAction> destinationCollection,
            Func<IClipboardDataControlPackage, Task<IEnumerable<IAction>>> mappingFunction)
        {
            this.destinationCollection = destinationCollection;
            this.mappingFunction = mappingFunction;

            sourceCollection.CollectionChanged += SourceCollection_CollectionChanged;
        }

        public void LoadFromKey(IClipboardDataControlPackage key)
        {
            destinationCollection.Clear();
            destinationCollection.Add(Default);

            this.key = key;
            foreach (var item in dictionaryStates[key])
            {
                destinationCollection.Add(item);
            }
        }

        private async void SourceCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems.Count > 0)
            {
                foreach (IClipboardDataControlPackage item in e.NewItems)
                {
                    PrepareDictionaryStateKey(item);
                    await AddResultsToDictionary(item);
                }
            }
        }

        private async Task AddResultsToDictionary(IClipboardDataControlPackage item)
        {
            var collection = dictionaryStates[item];
            var results = await mappingFunction(item);
            foreach (var result in results)
            {
                collection.Add(result);
                if (key == item)
                {
                    destinationCollection.Add(result);
                }
            }
        }

        private void PrepareDictionaryStateKey(IClipboardDataControlPackage item)
        {
            if (!dictionaryStates.ContainsKey(item))
            {
                dictionaryStates.Add(item, new HashSet<IAction>());
            }
        }
    }
}
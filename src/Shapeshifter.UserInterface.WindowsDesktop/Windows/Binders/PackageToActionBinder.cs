using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Binders.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.Binders
{
    internal class PackageToActionBinder : IAsyncListDictionaryBinder<IClipboardDataControlPackage, IAction>
    {
        private readonly IDictionary<IClipboardDataControlPackage, ICollection<IAction>> dictionaryStates;

        private IClipboardDataControlPackage currentKey;

        private ObservableCollection<IAction> boundDestinationCollection;
        private Func<IClipboardDataControlPackage, Task<IEnumerable<IAction>>> currentMappingFunction;

        public IAction Default { get; set; }

        public PackageToActionBinder()
        {
            dictionaryStates = new Dictionary<IClipboardDataControlPackage, ICollection<IAction>>();
        }
        
        public void Bind(
            ObservableCollection<IClipboardDataControlPackage> sourceCollection,
            ObservableCollection<IAction> destinationCollection,
            Func<IClipboardDataControlPackage, Task<IEnumerable<IAction>>> mappingFunction)
        {
            this.boundDestinationCollection = destinationCollection;
            this.currentMappingFunction = mappingFunction;

            sourceCollection.CollectionChanged += SourceCollection_CollectionChanged;
        }

        public void LoadFromKey(IClipboardDataControlPackage key)
        {
            boundDestinationCollection.Clear();
            boundDestinationCollection.Add(Default);

            this.currentKey = key;
            foreach (var item in dictionaryStates[key])
            {
                boundDestinationCollection.Add(item);
            }
        }

        private async void SourceCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems.Count <= 0) return;

            foreach (IClipboardDataControlPackage item in e.NewItems)
            {
                PrepareDictionaryStateKey(item);
                await AddResultsToDictionary(item);
            }
        }

        private async Task AddResultsToDictionary(IClipboardDataControlPackage item)
        {
            var collection = dictionaryStates[item];
            var results = await currentMappingFunction(item);
            foreach (var result in results)
            {
                collection.Add(result);
                if (currentKey == item)
                {
                    boundDestinationCollection.Add(result);
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
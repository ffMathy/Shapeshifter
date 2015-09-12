using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.Core.Actions;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows
{
    class PackageToActionBinder : IAsyncListDictionaryBinder<IClipboardDataControlPackage, IAction>
    {
        readonly IDictionary<IClipboardDataControlPackage, ICollection<IAction>> dictionaryStates;

        IClipboardDataControlPackage key;
        ObservableCollection<IAction> destinationCollection;
        Func<IClipboardDataControlPackage, Task<IEnumerable<IAction>>> mappingFunction;

        public IAction Default
        {
            get; set;
        }

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

        async void SourceCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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

        async Task AddResultsToDictionary(IClipboardDataControlPackage item)
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

        void PrepareDictionaryStateKey(IClipboardDataControlPackage item)
        {
            if (!dictionaryStates.ContainsKey(item))
            {
                dictionaryStates.Add(item, new HashSet<IAction>());
            }
        }
    }
}

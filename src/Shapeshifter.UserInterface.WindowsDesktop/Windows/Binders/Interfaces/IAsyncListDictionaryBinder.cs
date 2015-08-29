using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces
{
    interface IAsyncListDictionaryBinder<TKey, TData> where TKey : class
    {
        void Bind(
            ObservableCollection<TKey> sourceCollection, 
            ObservableCollection<TData> destinationCollection,
            Func<TKey, Task<IEnumerable<TData>>> mappingFunction);

        void LoadFromKey(TKey key);
    }
}

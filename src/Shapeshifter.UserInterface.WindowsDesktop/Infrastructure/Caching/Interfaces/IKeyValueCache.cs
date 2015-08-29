using System;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Caching.Interfaces
{
    interface IKeyValueCache<TKey, TValue>
    {
        TValue Get(TKey key);

        void Set(TKey key, TValue value);

        Task<TValue> ThunkifyAsync(TKey argument, Func<TKey, Task<TValue>> method);
    }
}

namespace Shapeshifter.WindowsDesktop.Infrastructure.Caching.Interfaces
{
    using System;
    using System.Threading.Tasks;

    public interface IKeyValueCache<TKey, TValue>
    {
        TValue Get(TKey key);

        void Set(TKey key, TValue value);

        TValue Thunkify(TKey argument, Func<TKey, TValue> method);

        Task<TValue> ThunkifyAsync(TKey argument, Func<TKey, Task<TValue>> method);
    }
}
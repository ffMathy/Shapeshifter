using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Caching.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Caching
{
    class KeyValueCache<TKey, TValue> : IKeyValueCache<TKey, TValue>
    {
        IDictionary<TKey, TValue> data;

        public KeyValueCache()
        {
            data = new Dictionary<TKey, TValue>();
        }

        public TValue Get(TKey key)
        {
            return data.ContainsKey(key) ? data[key] : default(TValue);
        }

        public void Set(TKey key, TValue value)
        {
            if(data.ContainsKey(key))
            {
                data[key] = value;
            } else
            {
                data.Add(key, value);
            }
        }

        public async Task<TValue> ThunkifyAsync(TKey argument, Func<TKey, Task<TValue>> method)
        {
            var cachedResult = Get(argument);
            if(!Equals(cachedResult, default(TValue)))
            {
                return cachedResult;
            }

            var result = await method(argument);
            Set(argument, result);

            return result;
        }
    }
}

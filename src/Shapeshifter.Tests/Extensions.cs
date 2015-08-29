using Autofac;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shapeshifter.Tests
{
    static class Extensions
    {
        static IDictionary<Type, object> fakeCache;

        static Extensions()
        {
            fakeCache = new Dictionary<Type, object>();
        }

        internal static void ClearCache()
        {
            fakeCache.Clear();
        }

        public static TInterface RegisterFake<TInterface>(this ContainerBuilder builder) where TInterface : class
        {
            if (!fakeCache.ContainsKey(typeof(TInterface)))
            {
                var fake = Substitute.For<TInterface>();
                fakeCache.Add(typeof(TInterface), fake);
                builder.RegisterInstance(fake);
                return fake;
            } else
            {
                return (TInterface)fakeCache[typeof(TInterface)];
            }
        }

        public static void IgnoreAwait(this Task task)
        {

        }
    }
}

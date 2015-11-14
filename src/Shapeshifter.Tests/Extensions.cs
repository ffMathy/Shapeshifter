using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using NSubstitute;

namespace Shapeshifter.Tests
{
    internal static class Extensions
    {
        private static readonly IDictionary<Type, object> fakeCache;

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
            if (fakeCache.ContainsKey(typeof (TInterface)))
                return (TInterface) fakeCache[typeof (TInterface)];

            var fake = Substitute.For<TInterface>();
            fakeCache.Add(typeof (TInterface), fake);
            builder.RegisterInstance(fake);
            return fake;
        }

        public static void IgnoreAwait(this Task task)
        {
        }
    }
}
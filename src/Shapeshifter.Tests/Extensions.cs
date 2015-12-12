namespace Shapeshifter.UserInterface.WindowsDesktop
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Autofac;

    using NSubstitute;

    static class Extensions
    {
        static readonly IDictionary<Type, object> fakeCache;

        static Extensions()
        {
            fakeCache = new Dictionary<Type, object>();
        }

        internal static void ClearCache()
        {
            fakeCache.Clear();
        }

        public static TInterface WithFakeSettings<TInterface>(this TInterface item, Action<TInterface> method)
        {
            method(item);
            return item;
        }

        public static TInterface RegisterFake<TInterface>(this ContainerBuilder builder)
            where TInterface : class
        {
            if (fakeCache.ContainsKey(typeof (TInterface)))
            {
                return (TInterface) fakeCache[typeof (TInterface)];
            }

            var fake = Substitute.For<TInterface>();
            return Register(builder, fake);
        }

        static TInterface Register<TInterface>(ContainerBuilder builder, TInterface fake) where TInterface : class
        {
            fakeCache.Add(typeof (TInterface), fake);
            builder.Register(c => fake)
                   .As<TInterface>();
            return fake;
        }

        public static void IgnoreAwait(this Task task) { }
    }
}
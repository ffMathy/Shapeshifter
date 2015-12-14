namespace Shapeshifter.WindowsDesktop
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        public static void AssertWait(Action expression)
        {
            AssertWait(10000, expression);
        }

        public static void AssertWait(int timeout, Action expression)
        {
            var time = DateTime.Now;

            var exceptions = new List<AssertFailedException>();
            while (
                ((DateTime.Now - time).TotalMilliseconds < timeout) ||
                (exceptions.Count == 0))
            {
                try
                {
                    expression();
                    return;
                }
                catch (AssertFailedException ex)
                {
                    if (exceptions.All(x => x.Message != ex.Message))
                    {
                        exceptions.Add(ex);
                    }
                }

                Thread.Sleep(1);
            }

            if (exceptions.Count > 1)
            {
                throw new AggregateException(exceptions);
            }

            throw exceptions.First();
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
            var collection = new ReadOnlyCollection<TInterface>(new List<TInterface>(new [] {fake}));

            Register(builder, fake);
            Register<IReadOnlyCollection<TInterface>>(builder, collection);
            Register<IEnumerable<TInterface>>(builder, collection);

            return fake;
        }

        static void Register<TInterface>(ContainerBuilder builder, TInterface fake) where TInterface : class
        {
            fakeCache.Add(typeof (TInterface), fake);
            builder.Register(c => fake)
                   .As<TInterface>()
                   .SingleInstance();
        }

        public static void IgnoreAwait(this Task task) { }
    }
}
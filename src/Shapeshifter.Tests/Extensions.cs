namespace Shapeshifter.UserInterface.WindowsDesktop
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Autofac;

    using NSubstitute;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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

            Exception lastException = null;
            while (
                ((DateTime.Now - time).TotalMilliseconds < timeout) || 
                (lastException == null))
            {
                try
                {
                    expression();
                    return;
                }
                catch (AssertFailedException ex)
                {
                    lastException = ex;
                }

                Thread.Sleep(1);
            }

            throw lastException;
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
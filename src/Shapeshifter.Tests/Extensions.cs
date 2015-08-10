using Autofac;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ploeh.AutoFixture;
using System.Reflection;
using Ploeh.AutoFixture.Kernel;

namespace Shapeshifter.Tests
{
    public static class Extensions
    {
        public static TInterface RegisterFake<TInterface>(this ContainerBuilder builder) where TInterface : class
        {
            var fake = Substitute.For<TInterface>();
            builder.RegisterInstance<TInterface>(fake);

            return fake;
        }

        public static void IgnoreAwait(this Task task)
        {

        }
    }
}

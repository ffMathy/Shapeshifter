using Autofac;
using NSubstitute;
using System.Threading.Tasks;

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

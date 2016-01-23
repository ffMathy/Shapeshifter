namespace Shapeshifter.WindowsDesktop.Services.Web
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Autofac;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DomainNameResolverTest: TestBase
    {
        [TestMethod]
        [TestCategory("Integration")]
        public async Task CanResolveGoogleDomain()
        {
            var container = CreateContainer();

            var resolver = container.Resolve<IDomainNameResolver>();

            var ipAddresses = await resolver.GetDomainIpAddressesAsync("google.com");
            var firstIpAddress = ipAddresses.First();

            Assert.IsNotNull(firstIpAddress);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task CanDetectGoogleAsValidDomain()
        {
            var container = CreateContainer();

            var resolver = container.Resolve<IDomainNameResolver>();
            var isValid = await resolver.IsValidDomainAsync("google.com");
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public async Task NoDomainSpecifiedThrowsException()
        {
            var container = CreateContainer();

            var resolver = container.Resolve<IDomainNameResolver>();
            await resolver.GetDomainIpAddressesAsync(null);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task IsNotValidWhenUsingGarbageText()
        {
            var container = CreateContainer();

            var resolver = container.Resolve<IDomainNameResolver>();
            var isValid = await resolver.IsValidDomainAsync("hello world foobar");
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task IsNotValidWithInvalidDomain()
        {
            var container = CreateContainer();

            var resolver = container.Resolve<IDomainNameResolver>();
            var isValid = await resolver.IsValidDomainAsync("fooooooooooooooooooo.com");
            Assert.IsFalse(isValid);
        }
    }
}
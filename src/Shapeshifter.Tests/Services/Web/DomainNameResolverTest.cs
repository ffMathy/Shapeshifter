namespace Shapeshifter.WindowsDesktop.Services.Web
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using Infrastructure.Caching.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DomainNameResolverTest: UnitTestFor<IDomainNameResolver>
    {
        public DomainNameResolverTest()
        {
            ExcludeFakeFor<IKeyValueCache<string, IPAddress[]>>();
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task CanResolveGoogleDomain()
        {
            var ipAddresses = await SystemUnderTest.GetDomainIpAddressesAsync(
                "google.com");
            var firstIpAddress = ipAddresses.First();

            Assert.IsNotNull(firstIpAddress);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task CanDetectGoogleAsValidDomain()
        {
            var isValid = await SystemUnderTest.IsValidDomainAsync(
                "google.com");
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public async Task NoDomainSpecifiedThrowsException()
        {
            await SystemUnderTest.GetDomainIpAddressesAsync(null);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task IsNotValidWhenUsingGarbageText()
        {
            var isValid = await SystemUnderTest.IsValidDomainAsync(
                "hello world foobar");
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task IsNotValidWithInvalidDomain()
        {
            var isValid = await SystemUnderTest.IsValidDomainAsync(
                "fooooooooooooooooooo.com");
            Assert.IsFalse(isValid);
        }
    }
}
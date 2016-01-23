namespace Shapeshifter.WindowsDesktop.Services.Web
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DomainNameResolverTest: UnitTestFor<IDomainNameResolver>
    {
        [TestMethod]
        [TestCategory("Integration")]
        public async Task CanResolveGoogleDomain()
        {
            var ipAddresses = await systemUnderTest.GetDomainIpAddressesAsync(
                "google.com");
            var firstIpAddress = ipAddresses.First();

            Assert.IsNotNull(firstIpAddress);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task CanDetectGoogleAsValidDomain()
        {
            var isValid = await systemUnderTest.IsValidDomainAsync(
                "google.com");
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public async Task NoDomainSpecifiedThrowsException()
        {
            await systemUnderTest.GetDomainIpAddressesAsync(null);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task IsNotValidWhenUsingGarbageText()
        {
            var isValid = await systemUnderTest.IsValidDomainAsync(
                "hello world foobar");
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task IsNotValidWithInvalidDomain()
        {
            var isValid = await systemUnderTest.IsValidDomainAsync(
                "fooooooooooooooooooo.com");
            Assert.IsFalse(isValid);
        }
    }
}
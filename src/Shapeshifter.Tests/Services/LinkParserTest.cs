using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System.Linq;
using Shapeshifter.UserInterface.WindowsDesktop.Services;
using System;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces;
using NSubstitute;

namespace Shapeshifter.Tests.Services
{
    [TestClass]
    public class LinkParserTest : TestBase
    {
        [TestMethod]
        public async Task ExtractsAllLinksFromText()
        {
            var container = CreateContainer();

            var text = "hello http://google.com world https://foo.com foobar blah.dk/hey/lol%20kitten.jpg lolz foobar.com www.baz.com test.net/news+list.txt?cat=pic&id=foo28";

            var linkParser = container.Resolve<ILinkParser>();
            var links = await linkParser.ExtractLinksFromTextAsync(text);

            Assert.IsTrue(links.Contains("http://google.com"));
            Assert.IsTrue(links.Contains("https://foo.com"));
            Assert.IsTrue(links.Contains("foobar.com"));
            Assert.IsTrue(links.Contains("www.baz.com"));
            Assert.IsTrue(links.Contains("test.net/news+list.txt?cat=pic&id=foo28"));
            Assert.IsTrue(links.Contains("blah.dk/hey/lol%20kitten.jpg"));
        }

        [TestMethod]
        public async Task HasLinkReturnsFalseWhenNoLinkPresent()
        {
            var container = CreateContainer();

            var text = "hello world";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.IsFalse(await linkParser.HasLinkAsync(text));
        }

        [TestMethod]
        public async Task HasLinkReturnsTrueWithoutProtocol()
        {
            var container = CreateContainer();

            var text = "hello google.com world";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.IsTrue(await linkParser.HasLinkAsync(text));
        }

        [TestMethod]
        public async Task LinkWithSubdomainIsValid()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IDomainNameResolver>()
                    .IsValidDomainAsync("foo.subdomain.google.com")
                    .Returns(Task.FromResult(true));
            });

            var text = "http://foo.subdomain.google.com";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.IsTrue(await linkParser.IsValidLinkAsync(text));
        }

        [TestMethod]
        public async Task LinkWithHttpProtocolIsValid()
        {
            var container = CreateContainer();

            var text = "http://google.com";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.IsTrue(await linkParser.IsValidLinkAsync(text));
        }

        [TestMethod]
        public async Task LinkWithHttpsProtocolIsValid()
        {
            var container = CreateContainer();
            
            var text = "https://google.com";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.IsTrue(await linkParser.IsValidLinkAsync(text));
        }

        [TestMethod]
        public async Task LinkWithParametersIsValid()
        {
            var container = CreateContainer();

            var text = "http://google.com?hello=flyp&version=1";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.IsTrue(await linkParser.IsValidLinkAsync(text));
        }

        [TestMethod]
        public async Task LinkWithDirectoriesIsValid()
        {
            var container = CreateContainer();

            var text = "http://google.com/foo/bar";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.IsTrue(await linkParser.IsValidLinkAsync(text));
        }

        [TestMethod]
        public void ImageLinkHasImageType()
        {
            var container = CreateContainer();

            var text = "google.com/foo/image.png";

            var linkParser = container.Resolve<ILinkParser>();
            var linkType = linkParser.GetLinkType(text);
            Assert.IsTrue(linkType.HasFlag(LinkType.ImageFile));
        }

        [TestMethod]
        public void HttpLinkHasHttpType()
        {
            var container = CreateContainer();

            var text = "http://google.com";

            var linkParser = container.Resolve<ILinkParser>();
            var linkType = linkParser.GetLinkType(text);
            Assert.IsTrue(linkType.HasFlag(LinkType.Http));
        }

        [TestMethod]
        public async Task SeveralLinksCanFindProperType()
        {
            var container = CreateContainer();

            var text = "http://google.com foo.com/img.jpg";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.IsTrue(await linkParser.HasLinkOfTypeAsync(text, LinkType.Http));
            Assert.IsTrue(await linkParser.HasLinkOfTypeAsync(text, LinkType.ImageFile));
        }

        [TestMethod]
        public void NormalLinkHasNoType()
        {
            var container = CreateContainer();

            var text = "google.com";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.AreEqual(LinkType.NoType, linkParser.GetLinkType(text));
        }

        [TestMethod]
        public void HttpsLinkHasHttpsType()
        {
            var container = CreateContainer();

            var text = "https://google.com";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.IsTrue(linkParser.GetLinkType(text).HasFlag(LinkType.Https));
        }
    }
}

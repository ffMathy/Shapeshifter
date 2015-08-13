using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System.Linq;
using Shapeshifter.UserInterface.WindowsDesktop.Services;
using System;

namespace Shapeshifter.Tests.Services
{
    [TestClass]
    public class LinkParserTest : TestBase
    {
        [TestMethod]
        public void ExtractsAllLinksFromText()
        {
            var container = CreateContainer();

            var text = "hello http://google.com world https://foo.com foobar blah.dk/hey/lol%20kitten.jpg lolz foobar.com test.net/news+list.txt?cat=pic&id=foo28";

            var linkParser = container.Resolve<ILinkParser>();
            var links = linkParser.ExtractLinksFromText(text);

            Assert.IsTrue(links.Contains("http://google.com"));
            Assert.IsTrue(links.Contains("https://foo.com"));
            Assert.IsTrue(links.Contains("foobar.com"));
            Assert.IsTrue(links.Contains("test.net/news+list.txt?cat=pic&id=foo28"));
            Assert.IsTrue(links.Contains("blah.dk/hey/lol%20kitten.jpg"));
        }

        [TestMethod]
        public void HasLinkReturnsFalseWhenNoLinkPresent()
        {
            var container = CreateContainer();

            var text = "hello world";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.IsFalse(linkParser.HasLink(text));
        }

        [TestMethod]
        public void HasLinkReturnsTrueWithoutProtocol()
        {
            var container = CreateContainer();

            var text = "hello google.com world";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.IsTrue(linkParser.HasLink(text));
        }

        [TestMethod]
        public void LinkWithSubdomainIsValid()
        {
            var container = CreateContainer();

            var text = "http://foo.subdomain.google.com";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.IsTrue(linkParser.IsValidLink(text));
        }

        [TestMethod]
        public void LinkWithHttpProtocolIsValid()
        {
            var container = CreateContainer();

            var text = "http://google.com";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.IsTrue(linkParser.IsValidLink(text));
        }

        [TestMethod]
        public void LinkWithHttpsProtocolIsValid()
        {
            var container = CreateContainer();
            
            var text = "https://google.com";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.IsTrue(linkParser.IsValidLink(text));
        }

        [TestMethod]
        public void LinkWithParametersIsValid()
        {
            var container = CreateContainer();

            var text = "http://google.com?hello=flyp&version=1";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.IsTrue(linkParser.IsValidLink(text));
        }

        [TestMethod]
        public void LinkWithDirectoriesIsValid()
        {
            var container = CreateContainer();

            var text = "http://google.com/foo/bar";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.IsTrue(linkParser.IsValidLink(text));
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
        public void SeveralLinksCanFindProperType()
        {
            var container = CreateContainer();

            var text = "http://google.com foo.com/img.jpg";

            var linkParser = container.Resolve<ILinkParser>();
            Assert.IsTrue(linkParser.HasLinkOfType(text, LinkType.Http));
            Assert.IsTrue(linkParser.HasLinkOfType(text, LinkType.ImageFile));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GettingTypeFromInvalidLinkThrowsException()
        {
            var container = CreateContainer();

            var text = "hello world";

            var linkParser = container.Resolve<ILinkParser>();
            linkParser.GetLinkType(text);
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

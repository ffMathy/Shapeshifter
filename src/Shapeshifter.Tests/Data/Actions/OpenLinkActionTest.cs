namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Autofac;

    using Data.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Interfaces;
    using Services.Web.Interfaces;

    [TestClass]
    public class OpenLinkActionTest: ActionTestBase<IOpenLinkAction>
    {
        [TestMethod]
        public void CanReadDescription()
        {
            Assert.IsNotNull(systemUnderTest.Description);
        }

        [TestMethod]
        public void CanReadTitle()
        {
            Assert.IsNotNull(systemUnderTest.Title);
        }

        [TestMethod]
        public async Task CanPerformIsFalseForNonTextTypes()
        {
            var someNonTextData = Substitute.For<IClipboardDataPackage>();
            Assert.IsFalse(
                await systemUnderTest.CanPerformAsync(
                    someNonTextData));
        }

        [TestMethod]
        public async Task CanPerformIsFalseForTextTypesWithNoLink()
        {
            container.Resolve<ILinkParser>()
             .HasLinkAsync(Arg.Any<string>())
             .Returns(Task.FromResult(false));

            var textDataWithLinkButNoImageLink = Substitute.For<IClipboardDataPackage>();
            
            Assert.IsFalse(await systemUnderTest.CanPerformAsync(textDataWithLinkButNoImageLink));
        }

        [TestMethod]
        public void OrderIsCorrect()
        {
            Assert.AreEqual(200, systemUnderTest.Order);
        }

        [TestMethod]
        public async Task PerformLaunchesDefaultBrowsersForEachLink()
        {
            container.Resolve<ILinkParser>()
             .HasLinkAsync(Arg.Any<string>())
             .Returns(Task.FromResult(true));

            container.Resolve<ILinkParser>()
             .ExtractLinksFromTextAsync(Arg.Any<string>())
             .Returns(
                 Task
                     .FromResult
                     <IReadOnlyCollection<string>>(
                         new[]
                         {
                                     "foo.com",
                                     "bar.com"
                         }));
            
            await systemUnderTest.PerformAsync(
                GetPackageContaining<IClipboardTextData>());

            var fakeProcessManager = container.Resolve<IProcessManager>();
            fakeProcessManager.Received(1)
                              .LaunchCommand("foo.com");
            fakeProcessManager.Received(1)
                              .LaunchCommand("bar.com");
        }
    }
}
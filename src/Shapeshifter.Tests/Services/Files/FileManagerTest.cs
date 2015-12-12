namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Files
{
    using System.IO;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Interfaces;

    using NSubstitute;

    using WindowsDesktop;

    [TestClass]
    public class FileManagerTest: TestBase
    {
        [TestMethod]
        [TestCategory("Integration")]
        public void CanCreateFilesAndDisposeThemAgain()
        {
            var container = CreateContainer();

            string permanentPath;
            string temporaryPath;
            using (var fileManager = container.Resolve<IFileManager>())
            {
                permanentPath = fileManager.PrepareFolder("Permanent");
                temporaryPath = fileManager.PrepareTemporaryFolder("Temporary");

                Assert.IsTrue(Directory.Exists(permanentPath));
                Assert.IsTrue(Directory.Exists(temporaryPath));
            }

            Assert.IsTrue(Directory.Exists(permanentPath));
            Assert.IsFalse(Directory.Exists(temporaryPath));

            Directory.Delete(permanentPath);

            Assert.IsFalse(Directory.Exists(permanentPath));
        }
    }
}
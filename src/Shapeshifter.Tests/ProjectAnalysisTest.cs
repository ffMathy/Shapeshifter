namespace Shapeshifter.WindowsDesktop
{
    using System.ComponentModel;
    using System.IO;
    using System.Xml;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ProjectAnalysisTest
    {
        [TestMethod]
        [Category("Integration")]
        public void ProjectFilesShouldCopyAllReferences()
        {
            var root = Extensions.GetSolutionRoot();
            var projectsToTest = new[]
            {
                "Shapeshifter.WindowsDesktop",
                "Shapeshifter.WindowsDesktop.Native"
            };

            foreach (var project in projectsToTest)
            {
                var projectPath = Path.Combine(
                    root,
                    project,
                    $"{project}.csproj");

                var document = new XmlDocument();
                document.LoadXml(
                    File.ReadAllText(projectPath));

                var namespaceManager = new XmlNamespaceManager(document.NameTable);
                namespaceManager.AddNamespace("default", "http://schemas.microsoft.com/developer/msbuild/2003");

                var references = document.SelectNodes("//default:Reference", namespaceManager);
                Assert.IsNotNull(references);
                Assert.AreNotEqual(0, references.Count);

                foreach (XmlNode reference in references)
                {
                    var privateNode = reference.SelectSingleNode("./default:Private", namespaceManager);
                    Assert.IsNotNull(privateNode);
                    Assert.AreEqual("True", privateNode.InnerText);
                }
            }
        }
    }
}
namespace Shapeshifter.WindowsDesktop
{
    using System.ComponentModel;
    using System.IO;
	using System.Linq;
	using System.Xml;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ProjectAnalysisTest : TestBase
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

                var references = document.SelectNodes("//default:Reference", namespaceManager).OfType<XmlNode>().ToArray();
				var projectReferences = document.SelectNodes("//default:ProjectReference", namespaceManager).OfType<XmlNode>().ToArray();

				var allReferences = references.Union(projectReferences).ToArray();
                Assert.AreNotEqual(0, allReferences.Length);

                foreach (var reference in allReferences)
                {
                    var privateNode = reference.SelectSingleNode("./default:Private", namespaceManager);
                    Assert.IsNotNull(privateNode, "A specific project reference (" + reference.InnerText + " in " + project + ") was not set to copy local.");
                    Assert.AreEqual("True", privateNode.InnerText);
                }
            }
        }
    }
}
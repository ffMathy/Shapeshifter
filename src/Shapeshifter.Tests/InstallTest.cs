using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shapeshifter.WindowsDesktop
{
	[TestClass]
	public class InstallTest : TestBase
	{
		[TestMethod]
		[Category("Integration")]
		public void CanInstallShapeshifter()
		{
			var directory = Environment.CurrentDirectory;
			Console.WriteLine("Working directory: " + directory);
			
			var rootPath = FindRootPathFromPath(directory);
			Console.WriteLine("Root path: " + rootPath);

			var applicationBuildPath = Path.Combine(rootPath, "build", "application");
			foreach(var file in Directory.GetFiles(applicationBuildPath)) {
				if(Path.GetFileName(file) != "Shapeshifter.exe") 
					continue;

				Console.WriteLine("Deleting file " + file);
				File.Delete(file);
			}

			Thread.Sleep(1000);

			Assert.AreEqual(1, Directory.GetFiles(applicationBuildPath).Length);
		}

		public string FindRootPathFromPath(string path) {
			var readmeFile = Path.Combine(path, "README.md");
			if(File.Exists(readmeFile))
				return path;
			
			return FindRootPathFromPath(new DirectoryInfo(path).Parent.FullName);
		}
	}
}

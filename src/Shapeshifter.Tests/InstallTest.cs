using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shapeshifter.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.WindowsDesktop
{
	[TestClass]
	public class InstallTest : TestBase
	{
		[TestMethod]
		[Category("Integration")]
		public void CanInstallShapeshifter()
		{
			var container = CreateContainer();

			var directory = Environment.CurrentDirectory;
			Console.WriteLine("Working directory: " + directory);
			
			var rootPath = FindRootPathFromPath(directory);
			Console.WriteLine("Root path: " + rootPath);

			var applicationBuildPath = Path.Combine(rootPath, "build", "application");

			const string executableName = "Shapeshifter.exe";

			foreach (var file in Directory.GetFiles(applicationBuildPath)) {
				if(Path.GetFileName(file) == executableName) 
					continue;

				Console.WriteLine("Deleting file " + file);
				File.Delete(file);
			}

			Thread.Sleep(1000);

			Assert.AreEqual(1, Directory.GetFiles(applicationBuildPath).Length);

			var settingsManager = container.Resolve<ISettingsManager>();
			settingsManager.SaveSetting<DateTime?>("LastLoad", null);

			var executablePath = Path.Combine(applicationBuildPath, executableName);
			var shapeshifterProcess = Process.Start(new ProcessStartInfo() {
				Arguments = null,
				WorkingDirectory = applicationBuildPath,
				FileName = executablePath
			});
			shapeshifterProcess.WaitForExit(10000);

			Thread.Sleep(10000);

			var lastLoad = settingsManager.LoadSetting<DateTime?>("LastLoad");
			Assert.IsNotNull(lastLoad);
		}

		public string FindRootPathFromPath(string path) {
			var readmeFile = Path.Combine(path, "README.md");
			if(File.Exists(readmeFile))
				return path;
			
			return FindRootPathFromPath(new DirectoryInfo(path).Parent.FullName);
		}
	}
}

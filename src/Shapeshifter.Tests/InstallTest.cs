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
		[Priority(int.MinValue)]
		[TestCategory("Predeploy")]
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

			Console.WriteLine("Launched Shapeshifter");

			shapeshifterProcess.WaitForInputIdle();

			Console.WriteLine("Waited for input idle");

			var now = DateTime.UtcNow;

			var lastLoad = (DateTime?)null;
			while(true) {
				lastLoad = settingsManager.LoadSetting<DateTime?>("LastLoad");
				if(lastLoad != null)
					break;

				if((DateTime.UtcNow - now).TotalMilliseconds > 10000)
					break;
					
				Thread.Sleep(1000);
			}

			Assert.IsNotNull(lastLoad);

			foreach(var process in Process.GetProcessesByName("Shapeshifter")) {
				process.Kill();
			}
		}

		public string FindRootPathFromPath(string path) {
			var readmeFile = Path.Combine(path, "README.md");
			if(File.Exists(readmeFile))
				return path;
			
			return FindRootPathFromPath(new DirectoryInfo(path).Parent.FullName);
		}
	}
}

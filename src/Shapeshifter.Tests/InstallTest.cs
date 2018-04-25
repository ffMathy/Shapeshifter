using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shapeshifter.WindowsDesktop.Services.Files;
using Shapeshifter.WindowsDesktop.Services.Files.Interfaces;
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
			var lastLoad = (DateTime?)null;

			try
			{
				var container = CreateContainer();

				var directory = Environment.CurrentDirectory;
				Console.WriteLine("Working directory: " + directory);

				var rootPath = FindRootPathFromPath(directory);
				Console.WriteLine("Root path: " + rootPath);

				var applicationBuildPath = Path.Combine(rootPath, "build", "application");

				const string executableName = "Shapeshifter.exe";

				foreach (var file in Directory.GetFiles(applicationBuildPath))
				{
					if (Path.GetFileName(file) == executableName)
						continue;

					Console.WriteLine("Deleting file " + file);
					File.Delete(file);
				}

				Thread.Sleep(1000);

				Assert.AreEqual(1, Directory.GetFiles(applicationBuildPath).Length);

				var settingsManager = container.Resolve<ISettingsManager>();
				settingsManager.SaveSetting<DateTime?>("LastLoad", null);

				var executablePath = Path.Combine(applicationBuildPath, executableName);
				var backupExecutablePath = executablePath + ".bak";

				File.Copy(executablePath, backupExecutablePath);

				var shapeshifterProcess = Process.Start(new ProcessStartInfo() {
					Arguments = null,
					WorkingDirectory = applicationBuildPath,
					FileName = executablePath
				});

				Console.WriteLine("Launched Shapeshifter");

				shapeshifterProcess.WaitForInputIdle();

				Console.WriteLine("Waited for input idle");

				var now = DateTime.UtcNow;

				while (true)
				{
					lastLoad = settingsManager.LoadSetting<DateTime?>("LastLoad");
					if (lastLoad != null)
						break;

					var elapsedTimeInSeconds = (int)(DateTime.UtcNow - now).TotalSeconds;
					if (elapsedTimeInSeconds > 60)
						break;

					Console.WriteLine("Waited " + elapsedTimeInSeconds + " seconds so far for Shapeshifter to launch after installation.");
					Thread.Sleep(1000);
				}

				Assert.IsFalse(File.Exists(executablePath));

				File.Move(backupExecutablePath, executablePath);
				Thread.Sleep(1000);

				Assert.IsTrue(File.Exists(executablePath));
			}
			finally
			{
				foreach (var process in Process.GetProcessesByName("Shapeshifter"))
				{
					process.Kill();
				}

				Thread.Sleep(1000);
			}

			var logFilePath = FileManager.GetFullPathFromTemporaryPath("Shapeshifter.log");
			File.Copy(logFilePath, "Log.txt");

			var logOutput = File.ReadAllLines("Log.txt");

			Console.WriteLine("Log output:");
			foreach(var line in logOutput) {
				Console.WriteLine(line);
			}

			foreach(var line in logOutput) {
				Assert.IsFalse(line.Contains("[ERR]"), line);
			}

			Assert.IsNotNull(lastLoad, "Install test failed.");
		}

		public string FindRootPathFromPath(string path)
		{
			var readmeFile = Path.Combine(path, "README.md");
			if (File.Exists(readmeFile))
				return path;

			return FindRootPathFromPath(new DirectoryInfo(path).Parent.FullName);
		}
	}
}

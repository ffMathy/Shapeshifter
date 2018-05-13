using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Shapeshifter.WindowsDesktop.Services.Files;
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
			try
			{
				var container = CreateContainer();

				const string executableName = "Shapeshifter.exe";

				var directory = Environment.CurrentDirectory;
				Console.WriteLine("Working directory: " + directory);

				var rootPath = FindRootPathFromPath(directory);
				Console.WriteLine("Root path: " + rootPath);

				var applicationBuildPath = Path.Combine(rootPath, "build", "application");

				var executablePath = Path.Combine(applicationBuildPath, executableName);
				var backupExecutablePath = executablePath + ".bak";

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

				File.Copy(executablePath, backupExecutablePath);

				var shapeshifterProcess = Process.Start(new ProcessStartInfo() {
					Arguments = "install",
					WorkingDirectory = applicationBuildPath,
					FileName = executablePath,
					RedirectStandardInput = true,
					RedirectStandardError = true,
					RedirectStandardOutput = true,
					UseShellExecute = false
				});

				Console.WriteLine("Launched Shapeshifter");

				shapeshifterProcess?.WaitForInputIdle();

				Console.WriteLine("Waited for input idle");

				var now = DateTime.UtcNow;

				DateTime? lastLoad;
				while (true)
				{
					lastLoad = settingsManager.LoadSetting<DateTime?>("LastLoad");
					if (lastLoad != null)
						break;

					var elapsedTimeInSeconds = (int)(DateTime.UtcNow - now).TotalSeconds;
					if (elapsedTimeInSeconds > 60 * 5)
						break;

					Console.WriteLine("Waited " + elapsedTimeInSeconds + " seconds so far for Shapeshifter to launch after installation.");
					Thread.Sleep(5000);
				}

				var logFilePath = FileManager.GetFullPathFromTemporaryPath($"Shapeshifter{DateTime.Now:yyyyMMdd}.log");
				File.Copy(logFilePath, "Log.txt");

				var logOutput = File.ReadAllLines("Log.txt");

				Console.WriteLine("Log output:");
				foreach (var line in logOutput)
				{
					Console.WriteLine(line);
				}

				Assert.IsFalse(File.Exists(executablePath), "The old executable at " + executablePath + " was not cleaned up after installation.");

				File.Move(backupExecutablePath, executablePath);
				Thread.Sleep(1000);

				Assert.IsTrue(File.Exists(executablePath));

				Assert.IsNotNull(lastLoad, "Install test failed.");

				foreach (var line in logOutput)
				{
					Assert.IsFalse(line.Contains("[ERR]"), line);
				}
			}
			finally
			{
				foreach (var process in Process.GetProcessesByName("Shapeshifter"))
				{
					process.Kill();
				}

				Thread.Sleep(1000);
			}
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

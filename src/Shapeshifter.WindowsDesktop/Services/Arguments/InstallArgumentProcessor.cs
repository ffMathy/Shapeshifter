namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Xml;

	using Controls.Window.Interfaces;
	using Controls.Window.ViewModels.Interfaces;

	using Interfaces;
	using Microsoft.Build.Evaluation;

	using Processes.Interfaces;

	using Properties;

	using Serilog;

	class InstallArgumentProcessor : IInstallArgumentProcessor
	{
		readonly IProcessManager processManager;
		readonly ISettingsViewModel settingsViewModel;
		readonly IMaintenanceWindow maintenanceWindow;
		readonly ILogger logger;

		public InstallArgumentProcessor(
			IProcessManager processManager,
			ISettingsViewModel settingsViewModel,
			IMaintenanceWindow maintenanceWindow,
			ILogger logger)
		{
			this.processManager = processManager;
			this.settingsViewModel = settingsViewModel;
			this.maintenanceWindow = maintenanceWindow;
			this.logger = logger;
		}

		public bool Terminates => true;

		internal static string TargetDirectory
		{
			get
			{
				var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
				return Path.Combine(programFilesPath, "Shapeshifter");
			}
		}

		static string TargetExecutableFile => Path.Combine(TargetDirectory, "Shapeshifter.exe");

		public bool CanProcess(string[] arguments) => arguments.Contains("install");

		public async Task ProcessAsync(string[] arguments)
		{
			logger.Information("Running installation procedure.");
			Install();
		}

		void Install()
		{
			maintenanceWindow.Show("Installing ...");

			PrepareInstallDirectory();
			InstallToInstallDirectory();

			ConfigureDefaultSettings();

			logger.Information("Default settings have been configured.");

			LaunchInstalledExecutable(
				processManager.GetCurrentProcessFilePath());

			logger.Information("Launched installed executable.");
		}

		void InstallToInstallDirectory()
		{
			WriteExecutable();
			WriteDependencies();
			WriteApplicationConfiguration();
			WriteApplicationManifest();
			WriteApplicationDebugInformation();
			RunNativeGeneration();

			logger.Information("Executable, configuration and manifest written to install directory.");
		}

		void RunNativeGeneration()
		{
			var process = processManager.LaunchFileWithAdministrativeRights(
				@"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\ngen.exe",
				"install \"" + TargetExecutableFile + "\" /ExeConfig:\"" + TargetExecutableFile + "\"");
			process.WaitForExit();

			if (process.ExitCode != 0)
				throw new Exception("Could not generate a native image of the installed executable.");
		}

		void WriteDependencies()
		{
			WriteEasyHookDependencies();

			using (var textReader = new StringReader(Resources.ProjectFile))
			using (var reader = XmlReader.Create(textReader))
			{
				var project = new Project(reader);

				var projectReferences = project.Items
					.Where(x => x.ItemType == "ProjectReference")
					.Select(x => x.EvaluatedInclude)
					.Select(Path.GetFileNameWithoutExtension)
					.ToArray();

				var assemblyReferences = project.Items
					.Where(x => x.ItemType == "Reference")
					.Select(x => x.EvaluatedInclude)
					.Select(x => x.Split(',').First())
					.ToArray();

				var allReferences = projectReferences
					.Union(assemblyReferences)
					.ToArray();
				foreach (var reference in allReferences)
				{
					EmitCosturaResourceToDisk(reference + ".dll");
				}
			}
		}

		void WriteEasyHookDependencies()
		{
			var dependencyPrefix = $"{nameof(Shapeshifter)}.{nameof(WindowsDesktop)}.";
			var dependenciesToSave = new List<string>
			{
				dependencyPrefix + $"EasyHook64Svc.exe",
				dependencyPrefix + $"EasyHook64.dll",
				dependencyPrefix + $"EasyLoad64.dll"
			};

			foreach (var dependency in dependenciesToSave)
			{
				EmitEmbeddedResourceToDisk(
					dependency,
					dependency.Substring(
						dependencyPrefix.Length));
			}
		}

		void EmitCosturaResourceToDisk(string targetFile)
		{
			EmitEmbeddedResourceToDisk("costura." + targetFile.ToLower(), targetFile);
		}

		void EmitEmbeddedResourceToDisk(string targetResourceName, string targetFile)
		{
			var stream = Application.ResourceAssembly.GetManifestResourceStream(targetResourceName);
			if (stream == null)
				return;

			logger.Verbose("Attempting to write resource {resourceName} to {embeddedFile}.", targetResourceName, targetFile);
			using (stream)
			{
				var bytes = new byte[stream.Length];
				stream.Read(bytes, 0, bytes.Length);

				logger.Verbose("Resource {resourceName} of {length} bytes written to {embeddedFile}.", targetResourceName, bytes.Length, targetFile);
				File.WriteAllBytes(
					Path.Combine(
						TargetDirectory,
						targetFile),
					bytes);
			}
		}

		static void WriteApplicationManifest()
		{
			File.WriteAllBytes(
				Path.Combine(
					TargetDirectory,
					"Shapeshifter.manifest"),
				Resources.AppManifest);
		}

		static void WriteApplicationDebugInformation()
		{
			File.WriteAllBytes(
				Path.Combine(
					TargetDirectory,
					"Shapeshifter.pdb"),
				Resources.AppDebugFile);
		}

		static void WriteApplicationConfiguration()
		{
			File.WriteAllText(
				Path.Combine(
					TargetDirectory,
					"Shapeshifter.exe.config"),
				Resources.AppConfiguration);
		}

		void ConfigureDefaultSettings()
		{
			settingsViewModel.StartWithWindows = true;
		}

		void LaunchInstalledExecutable(string currentExecutableFile)
		{
			processManager.LaunchFileWithAdministrativeRights(TargetExecutableFile, $"postinstall \"{currentExecutableFile}\"");
		}

		void WriteExecutable()
		{
			File.Copy(
				processManager.GetCurrentProcessFilePath(),
				TargetExecutableFile,
				true);
		}

		void PrepareInstallDirectory()
		{
			logger.Information("Target install directory is " + TargetDirectory + ".");

			if (Directory.Exists(TargetDirectory))
				Directory.Delete(TargetDirectory, true);

			Directory.CreateDirectory(TargetDirectory);

			logger.Information("Install directory prepared.");
		}
	}
}

namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows;

	using Controls.Window.Interfaces;
	using Controls.Window.ViewModels.Interfaces;

	using Interfaces;

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
			maintenanceWindow.Show("Installing Shapeshifter ...");

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

			logger.Information("Executable, configuration and manifest written to install directory.");
		}

		void WriteDependencies()
		{
			WriteEasyHookDependencies();
			
			EmitCosturaResourceToDisk($"{nameof(Shapeshifter)}.{nameof(WindowsDesktop)}.{nameof(KeyboardHookInterception)}.dll");
			EmitCosturaResourceToDisk($"{nameof(Shapeshifter)}.{nameof(WindowsDesktop)}.{nameof(Native)}.dll");

			//var project = new Project();
			//project.LoadXml(Resources.ProjectFile);

			//var embeddedResources =
			//	from grp in project.ItemGroups.Cast<BuildItemGroup>()
			//	from item in grp.Cast<BuildItem>()
			//	where item.Name == "EmbeddedResource"
			//	select item;

			//foreach (BuildItem item in embeddedResources)
			//{
			//	Console.WriteLine(item.Include); // prints the name of the resource file
			//}
		}

		void WriteEasyHookDependencies()
		{
			var processorArchitecture = Environment.Is64BitOperatingSystem ? "64" : "32";
			var dependencyPrefix = $"{nameof(Shapeshifter)}.{nameof(WindowsDesktop)}.";

			var dependenciesToSave = new List<string>
			{
					dependencyPrefix + $"EasyHook{processorArchitecture}Svc.exe",
					dependencyPrefix + $"EasyHook{processorArchitecture}.dll",
					dependencyPrefix + $"EasyLoad{processorArchitecture}.dll"
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
			logger.Verbose("Attempting to write resource {resourceName} to {embeddedFile}.", targetResourceName, targetFile);
			using (var stream = Application.ResourceAssembly.GetManifestResourceStream(targetResourceName))
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
					"Shapeshifter.config"),
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

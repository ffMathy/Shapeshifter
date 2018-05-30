namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Xml;

    using Controls.Window.Interfaces;
    using Controls.Window.ViewModels.Interfaces;

    using Files.Interfaces;

    using Information;

    using Interfaces;

    using Processes.Interfaces;

    using Properties;

    using Serilog;

    class InstallArgumentProcessor : IInstallArgumentProcessor
    {
        readonly IProcessManager processManager;
        readonly IFileManager fileManager;
        readonly ISettingsViewModel settingsViewModel;
        readonly IMaintenanceWindow maintenanceWindow;
        readonly ILogger logger;

        public InstallArgumentProcessor(
            IProcessManager processManager,
            IFileManager fileManager,
            ISettingsViewModel settingsViewModel,
            IMaintenanceWindow maintenanceWindow,
            ILogger logger)
        {
            this.processManager = processManager;
            this.fileManager = fileManager;
            this.settingsViewModel = settingsViewModel;
            this.maintenanceWindow = maintenanceWindow;
            this.logger = logger;
        }

        public bool Terminates => true;

        public bool CanProcess(string[] arguments) => arguments.Contains("install");

        public async Task ProcessAsync(string[] arguments)
        {
            logger.Information("Running installation procedure.");
            await InstallAsync();
        }

        async Task InstallAsync()
        {
            maintenanceWindow.Show("Installing ...");

            PrepareInstallDirectory();
            await InstallToInstallDirectoryAsync();

            ConfigureDefaultSettings();

            logger.Information("Default settings have been configured.");

            LaunchInstalledExecutable(
                CurrentProcessInformation.GetCurrentProcessFilePath());

            logger.Information("Launched installed executable.");
        }

        async Task InstallToInstallDirectoryAsync()
        {
            await WriteExecutableAsync();
            WriteDependencies();
            WriteApplicationConfiguration();
            WriteApplicationManifest();
            WriteApplicationDebugInformation();

            var exitCode = await RunNativeGenerationAsync();
            if (exitCode != 0)
                throw new Exception("Could not generate a native image of the installed executable.");

            logger.Information("Executable, configuration and manifest written to install directory.");
        }

        Task<int> RunNativeGenerationAsync()
        {
            var process = processManager.LaunchFile(
                @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\ngen.exe",
                @"install """ + InstallationInformation.TargetExecutableFile + @""" /ExeConfig:""" + InstallationInformation.TargetExecutableFile + @""" /nologo",
                ProcessWindowStyle.Hidden);

            var taskCompletionSource = new TaskCompletionSource<int>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => taskCompletionSource.TrySetResult(process.ExitCode);

            process.Refresh();

            var linesOutput = process
                .StandardOutput
                .ReadToEnd()
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .ToArray();
            foreach (var line in linesOutput)
            {
                if (line.StartsWith("Failed"))
                {
                    logger.Error(line);
                }
                else
                {
                    logger.Verbose(line);
                }
            }

            if (process.HasExited)
                return Task.FromResult(process.ExitCode);

            return taskCompletionSource.Task;
        }

        void WriteDependencies()
        {
            var document = new XmlDocument();
            document.LoadXml(Resources.ProjectFile);

            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace("default", "http://schemas.microsoft.com/developer/msbuild/2003");

            var references = document.SelectNodes("//default:Reference", namespaceManager).OfType<XmlNode>().ToArray();
            var projectReferences = document.SelectNodes("//default:ProjectReference", namespaceManager).OfType<XmlNode>().ToArray();

            var allReferences = references.Union(projectReferences).ToArray();
            foreach (var referenceNode in allReferences)
            {
                var hintPath = referenceNode.SelectSingleNode("./default:HintPath", namespaceManager)?.InnerText;
                if (string.IsNullOrEmpty(hintPath))
                    continue;

				logger.Verbose("Analyzing reference with hint path {hintPath}.", hintPath);

                //ignore windows SDK files because they are not supported in Costura.
                if (Path.GetExtension(hintPath) == ".winmd")
                    continue;

				var include = referenceNode.Attributes["Include"].Value;
                var reference = include
                    .Split(',')
                    .First();
                EmitCosturaResourceToDisk(reference);
            }

            WriteEasyHookDependencies();
        }

        void WriteEasyHookDependencies()
        {
            var dependencyPrefix = $"{nameof(Shapeshifter)}.{nameof(WindowsDesktop)}.";
            var dependenciesToSave = new List<string>
            {
                dependencyPrefix + "EasyHook64Svc.exe",
                dependencyPrefix + "EasyHook64.dll",
                dependencyPrefix + "EasyLoad64.dll"
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
            EmitEmbeddedResourceToDisk("costura." + targetFile.ToLower(), targetFile + ".dll");
        }

        void EmitEmbeddedResourceToDisk(string targetResourceName, string targetFile)
		{
            var stream = Application.ResourceAssembly.GetManifestResourceStream(targetResourceName);
            if (stream == null)
            {
                if (!targetFile.StartsWith(nameof(System) + "."))
                    throw new Exception("Could not emit embedded resource " + targetResourceName + " as " + targetFile + ".");

                logger.Verbose("Embedded system assembly {name} resource was not found.", targetFile);
                return;
            }

            logger.Verbose("Attempting to write resource {resourceName} to {embeddedFile}.", targetResourceName, targetFile);
            using (stream)
            {
                Debug.Assert(stream != null, nameof(stream) + " != null");

                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);

                logger.Verbose("Resource {resourceName} of {length} bytes written to {embeddedFile}.", targetResourceName, bytes.Length, targetFile);
                File.WriteAllBytes(
                    Path.Combine(
                        InstallationInformation.TargetDirectory,
                        targetFile),
                    bytes);
            }
        }

        static void WriteApplicationManifest()
        {
            File.WriteAllBytes(
                Path.Combine(
                    InstallationInformation.TargetDirectory,
                    $"{nameof(Shapeshifter)}.exe.manifest"),
                Resources.AppManifest);
        }

        static void WriteApplicationDebugInformation()
        {
            File.WriteAllBytes(
                Path.Combine(
                    InstallationInformation.TargetDirectory,
                    $"{nameof(Shapeshifter)}.pdb"),
                Resources.AppDebugFile);
        }

        static void WriteApplicationConfiguration()
        {
            File.WriteAllText(
                Path.Combine(
                    InstallationInformation.TargetDirectory,
                    $"{nameof(Shapeshifter)}.exe.config"),
                Resources.AppConfiguration);
        }

        void ConfigureDefaultSettings()
        {
            settingsViewModel.StartWithWindows = true;
        }

        void LaunchInstalledExecutable(string currentExecutableFile)
        {
            try
            {
                processManager.LaunchFileWithAdministrativeRights(InstallationInformation.TargetExecutableFile, $"postinstall \"{currentExecutableFile}\"");
            }
            catch (Win32Exception)
            {
                processManager.CloseCurrentProcess();
            }
        }

        async Task WriteExecutableAsync()
        {
            await fileManager.CopyFileAsync(
                CurrentProcessInformation.GetCurrentProcessFilePath(),
                InstallationInformation.TargetExecutableFile);
        }

        void PrepareInstallDirectory()
        {
            logger.Information("Target install directory is " + InstallationInformation.TargetDirectory + ".");

            if (Directory.Exists(InstallationInformation.TargetDirectory))
                Directory.Delete(InstallationInformation.TargetDirectory, true);

            Directory.CreateDirectory(InstallationInformation.TargetDirectory);

            logger.Information("Install directory prepared.");
        }
    }
}

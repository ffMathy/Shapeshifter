namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    using Interfaces;

    using JetBrains.Annotations;

    using Services.Interfaces;
    using Services.Processes.Interfaces;

    class SettingsViewModel: ISettingsViewModel
    {
        readonly IRegistryManager registryManager;
        readonly IProcessManager processManager;

        public event PropertyChangedEventHandler PropertyChanged;

        public SettingsViewModel(
            IRegistryManager registryManager,
            IProcessManager processManager)
        {
            this.registryManager = registryManager;
            this.processManager = processManager;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool StartWithWindows
        {
            get
            {
                var runRegistryPath = GetRunRegistryPath();
                return registryManager.GetValue(
                    runRegistryPath,
                    nameof(Shapeshifter)) != null;
            }
            set
            {
                if (value == StartWithWindows)
                {
                    return;
                }

                var runRegistryPath = GetRunRegistryPath();
                if (value)
                {
                    var currentExecutablePath = processManager.GetCurrentProcessPath();
                    registryManager.AddValue(
                        runRegistryPath,
                        nameof(Shapeshifter),
                        $@"""{currentExecutablePath}""");
                }
                else
                {
                    registryManager.RemoveValue(
                        runRegistryPath,
                        nameof(Shapeshifter));
                }

                OnPropertyChanged();
            }
        }

        static string GetRunRegistryPath()
        {
            return @"Software\Microsoft\Windows\CurrentVersion\Run";
        }
    }
}
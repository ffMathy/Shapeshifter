﻿namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Input;

    using Interfaces;

    using Services.Interfaces;
    using Services.Keyboard.Interfaces;
    using Services.Processes.Interfaces;

    class SettingsViewModel: ISettingsViewModel
    {
        readonly IRegistryManager registryManager;
        readonly IProcessManager processManager;
        readonly ISettingsManager settingsManager;
        readonly IKeyboardManager keyboardManager;

        int pasteDurationBeforeUserInterfaceShowsInMilliseconds;
        int maximumAmountOfItemsInClipboard;
        string hotkeyString;

        public event PropertyChangedEventHandler PropertyChanged;

        public SettingsViewModel(
            IRegistryManager registryManager,
            IProcessManager processManager,
            ISettingsManager settingsManager,
            IKeyboardManager keyboardManager)
        {
            this.registryManager = registryManager;
            this.processManager = processManager;
            this.settingsManager = settingsManager;
            this.keyboardManager = keyboardManager;

            pasteDurationBeforeUserInterfaceShowsInMilliseconds = settingsManager.LoadSetting(
                nameof(PasteDurationBeforeUserInterfaceShowsInMilliseconds),
                300);
            maximumAmountOfItemsInClipboard = settingsManager.LoadSetting(
                nameof(MaximumAmountOfItemsInClipboard),
                8);
        }
        
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
                    var currentExecutablePath = processManager.GetCurrentProcessFilePath();
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

        public int PasteDurationBeforeUserInterfaceShowsInMilliseconds
        {
            get
            {
                return pasteDurationBeforeUserInterfaceShowsInMilliseconds;
            }
            set
            {
                if (value == PasteDurationBeforeUserInterfaceShowsInMilliseconds) return;

                settingsManager.SaveSetting(
                    nameof(PasteDurationBeforeUserInterfaceShowsInMilliseconds),
                    pasteDurationBeforeUserInterfaceShowsInMilliseconds = value);
                
                OnPropertyChanged();
            }
        }

        public int MaximumAmountOfItemsInClipboard
        {
            get
            {
                return maximumAmountOfItemsInClipboard;
            }
            set
            {
                if (value == maximumAmountOfItemsInClipboard) return;
                settingsManager.SaveSetting(
                    nameof(MaximumAmountOfItemsInClipboard),
                    maximumAmountOfItemsInClipboard = value);
                OnPropertyChanged();
            }
        }

        public string HotkeyString
        {
            get
            {
                return hotkeyString ?? "Ctrl + V";
            }
            private set
            {
                if (value == hotkeyString) return;
                hotkeyString = value;
                OnPropertyChanged();
            }
        }

        public void OnReceiveKeyDown(Key key)
        {
            var isModifierKey =
                (key == Key.LeftShift) ||
                (key == Key.RightShift) ||
                (key == Key.LWin) || 
                (key == Key.RWin) || 
                (key == Key.LeftCtrl) || 
                (key == Key.RightCtrl) || 
                (key == Key.LeftAlt) ||
                (key == Key.RightAlt);
            if (isModifierKey)
            {
                return;
            }

            var result = string.Empty;

            if (keyboardManager.IsKeyDown(Key.LeftShift) || keyboardManager.IsKeyDown(Key.RightShift))
            {
                result += "Shift + ";
            }

            if (keyboardManager.IsKeyDown(Key.LWin) || keyboardManager.IsKeyDown(Key.RWin))
            {
                result += "Win + ";
            }

            if (keyboardManager.IsKeyDown(Key.LeftCtrl) || keyboardManager.IsKeyDown(Key.RightCtrl))
            {
                result += "Ctrl + ";
            }

            if (keyboardManager.IsKeyDown(Key.LeftAlt) || keyboardManager.IsKeyDown(Key.RightAlt))
            {
                result += "Alt + ";
            }

            result += key.ToString();

            HotkeyString = result;
        }

        static string GetRunRegistryPath()
        {
            return @"Software\Microsoft\Windows\CurrentVersion\Run";
        }
    }
}
using Microsoft.Win32;
using Shapeshifter.WindowsDesktop.Services.Interfaces;

using System.Collections.Generic;

namespace Shapeshifter.WindowsDesktop.Services
{
    class RegistryManager: IRegistryManager
    {
        public void AddKey(string path, string keyName)
        {
            var registryKey = Registry
                .CurrentUser
                .CreateSubKey(path);
            registryKey?.Dispose();
        }

        public IReadOnlyCollection<string> GetKeys(string path)
        {
            using (var registryKey = Registry
                .CurrentUser
                .OpenSubKey(path))
            {
                return registryKey?.GetSubKeyNames();
            }
        }

        public void RemoveKey(string path, string keyName)
        {
            Registry.CurrentUser.DeleteSubKeyTree(path);
        }

        public string GetValue(string path, string valueName)
        {
            using (var registryKey = Registry
                .CurrentUser
                .OpenSubKey(path))
            {
                return (string)registryKey?.GetValue(valueName);
            }
        }

        public void AddValue(string path, string valueName, string value)
        {
            using (var registryKey = Registry
                .CurrentUser
                .OpenSubKey(path, true))
            {
                registryKey?.SetValue(valueName, value);
            }
        }

        public IReadOnlyCollection<string> GetValueNames(string path)
        {
            using (var registryKey = Registry
                .CurrentUser
                .OpenSubKey(path, true))
            {
                return registryKey?.GetValueNames();
            }
        }

        public void RemoveValue(string path, string valueName)
        {
            using (var registryKey = Registry
                .CurrentUser
                .OpenSubKey(path, true))
            {
                registryKey?.DeleteValue(valueName);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Services
{
    using Files.Interfaces;

    using Interfaces;
    using System.IO;

    using Newtonsoft.Json;

    class SettingsManager : ISettingsManager
    {
        readonly string settingsPath;

        public SettingsManager(
            IFileManager fileManager)
        {
            settingsPath = fileManager.PrepareFolder("Settings");
        }

        public void SaveSetting<T>(string key, T value)
        {
            var path = GetPathForKey(key);
            var json = JsonConvert.SerializeObject(value);
            File.WriteAllText(path, json);
        }

        string GetPathForKey(string key)
        {
            return Path.Combine(settingsPath, $"{key}.json");
        }

        public T LoadSetting<T>(string key)
        {
            var path = GetPathForKey(key);
            var json = File.ReadAllText(path);
            var value = JsonConvert.DeserializeObject<T>(json);
            return value;
        }
    }
}

namespace Shapeshifter.WindowsDesktop.Services
{
    using System.IO;

    using Files.Interfaces;

    using Interfaces;

    using Newtonsoft.Json;

    class SettingsManager: ISettingsManager
    {
        readonly string settingsPath;

        public SettingsManager(
            IFileManager fileManager)
        {
            settingsPath = fileManager.PrepareIsolatedFolder("Settings");
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

        public T LoadSetting<T>(string key, T defaultValue = default(T))
        {
            var path = GetPathForKey(key);
            if (!File.Exists(path))
            {
                return defaultValue;
            }

            var json = File.ReadAllText(path);

            var value = JsonConvert.DeserializeObject<T>(json);
            return value;
        }
    }
}
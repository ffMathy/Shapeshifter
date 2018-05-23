namespace Shapeshifter.Website.Logic
{
	using System.IO;

	using Newtonsoft.Json;

	class SettingsManager: ISettingsManager
	{
		public void SaveSetting<T>(string key, T value)
		{
			File.WriteAllText(key + ".settings", JsonConvert.SerializeObject(value));
		}

		public T LoadSetting<T>(string key, T defaultValue = default(T))
		{
			if (!File.Exists(key + ".settings"))
				return defaultValue;

			return JsonConvert.DeserializeObject<T>(File.ReadAllText(key + ".settings"));
		}
	}
}
namespace Shapeshifter.Website.Logic
{
	using System.Diagnostics;
	using System.Linq;

	using Microsoft.Win32;

	using Newtonsoft.Json;

	class SettingsManager: ISettingsManager
	{
		public void SaveSetting<T>(string key, T value)
		{
			using (var registryKey = OpenShapeshifterKey())
			{
				registryKey.SetValue(key, JsonConvert.SerializeObject(value));
			}
		}

		public T LoadSetting<T>(string key, T defaultValue = default(T))
		{
			using (var registryKey = OpenShapeshifterKey())
			{
				var value = (string)registryKey.GetValue(key, string.Empty);
				if (string.IsNullOrEmpty(value))
					return defaultValue;

				return JsonConvert.DeserializeObject<T>(value);
			}
		}

		static RegistryKey OpenShapeshifterKey()
		{
			using (var softwareKey = Registry.CurrentUser.OpenSubKey("Software", RegistryKeyPermissionCheck.ReadWriteSubTree))
			{
				Debug.Assert(softwareKey != null, nameof(softwareKey) + " != null");

				var keyNames = softwareKey.GetSubKeyNames();
				if (!keyNames.Contains(nameof(Shapeshifter)))
					return softwareKey.CreateSubKey(nameof(Shapeshifter), RegistryKeyPermissionCheck.ReadWriteSubTree);

				return softwareKey.OpenSubKey(nameof(Shapeshifter), RegistryKeyPermissionCheck.ReadWriteSubTree);
			}
		}
	}
}
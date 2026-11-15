namespace Shapeshifter.Website.Logic;

using Newtonsoft.Json;

/// <summary>
/// Persists small runtime values (such as rotated Patreon tokens) as <c>&lt;key&gt;.settings</c>
/// JSON files in the application's content root.
/// </summary>
public sealed class SettingsManager(IHostEnvironment environment) : ISettingsManager
{
	readonly Lock _lock = new();

	public void SaveSetting<T>(string key, T value)
	{
		lock (_lock)
		{
			File.WriteAllText(GetPath(key), JsonConvert.SerializeObject(value));
		}
	}

	public T? LoadSetting<T>(string key, T? defaultValue = default)
	{
		lock (_lock)
		{
			var path = GetPath(key);
			if (!File.Exists(path))
				return defaultValue;

			return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
		}
	}

	string GetPath(string key)
	{
		if (key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || key.Contains("..", StringComparison.Ordinal))
			throw new ArgumentException("Invalid setting key.", nameof(key));

		return Path.Combine(environment.ContentRootPath, key + ".settings");
	}
}

namespace Shapeshifter.Website.Logic
{
	public interface ISettingsManager
	{
		void SaveSetting<T>(string key, T value);

		T LoadSetting<T>(string key, T defaultValue = default(T));
	}
}
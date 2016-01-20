namespace Shapeshifter.WindowsDesktop.Services.Interfaces
{
    using System.Collections.Generic;

    public interface IRegistryManager
    {
        string AddKey(string path, string keyName);

        IReadOnlyCollection<string> GetKeys(string path);

        string RemoveKey(string path, string keyName);

        string GetValue(string path, string valueName);

        void AddValue(string path, string valueName, string value);

        IReadOnlyCollection<string> GetValues(string path);

        string RemoveValue(string path, string valueName);
    }
}
namespace Shapeshifter.WindowsDesktop.Services.Interfaces
{
    using System.Collections.Generic;

    public interface IRegistryManager
    {
        void AddKey(string path, string keyName);

        IReadOnlyCollection<string> GetKeys(string path);

        void RemoveKey(string path, string keyName);

        string GetValue(string path, string valueName);

        void AddValue(string path, string valueName, string value);

        IReadOnlyCollection<string> GetValueNames(string path);

        void RemoveValue(string path, string valueName);
    }
}
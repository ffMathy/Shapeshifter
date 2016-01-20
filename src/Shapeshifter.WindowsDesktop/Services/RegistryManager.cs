using Shapeshifter.WindowsDesktop.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Services
{
    class RegistryManager: IRegistryManager
    {
        public string AddKey(string path, string keyName)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<string> GetKeys(string path)
        {
            throw new NotImplementedException();
        }

        public string RemoveKey(string path, string keyName)
        {
            throw new NotImplementedException();
        }

        public string GetValue(string path, string valueName)
        {
            throw new NotImplementedException();
        }

        public void AddValue(string path, string valueName, string value)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<string> GetValues(string path)
        {
            throw new NotImplementedException();
        }

        public string RemoveValue(string path, string valueName)
        {
            throw new NotImplementedException();
        }
    }
}

using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Files
{
    class FileManager : IFileManager, IDisposable
    {
        private readonly ICollection<string> temporaryPaths;

        public FileManager()
        {
            temporaryPaths = new HashSet<string>();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public string PrepareTemporaryPath(string path)
        {
            temporaryPaths.Add(path);
            throw new NotImplementedException();
        }

        public void WriteBytesToFile(string fileName, byte[] bytes)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces
{
    public interface IFileTypeInterpreter
    {
        FileType GetFileTypeFromFileName(string name);
    }
}

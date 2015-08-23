using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces
{
    public interface IEnvironmentInformation
    {
        bool IsInDesignTime { get; }
    }
}

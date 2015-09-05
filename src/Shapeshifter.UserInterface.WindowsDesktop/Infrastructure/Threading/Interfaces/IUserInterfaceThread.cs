using System;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    interface IUserInterfaceThread
    {
        void Invoke(Action action);
    }
}

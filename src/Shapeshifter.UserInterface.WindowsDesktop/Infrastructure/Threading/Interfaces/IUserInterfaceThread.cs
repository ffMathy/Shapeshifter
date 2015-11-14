using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    internal interface IUserInterfaceThread
    {
        void Invoke(Action action);
    }
}
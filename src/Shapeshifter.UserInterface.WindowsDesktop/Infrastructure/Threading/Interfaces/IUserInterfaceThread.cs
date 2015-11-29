namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    using System;

    interface IUserInterfaceThread
    {
        void Invoke(Action action);
    }
}
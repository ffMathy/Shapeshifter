namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    using System;

    interface IUserInterfaceThread
    {
        void Invoke(Action action);
    }
}
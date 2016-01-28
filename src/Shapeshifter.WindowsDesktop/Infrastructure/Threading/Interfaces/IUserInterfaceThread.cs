namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    using System;

    public interface IUserInterfaceThread
    {
        void Invoke(Action action);
    }
}
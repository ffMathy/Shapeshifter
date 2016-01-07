namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    using System;

    using Shared.Infrastructure.Dependencies.Interfaces;

    public interface IMainThreadInvoker: ISingleInstance
    {
        void Invoke(Action action);
    }
}
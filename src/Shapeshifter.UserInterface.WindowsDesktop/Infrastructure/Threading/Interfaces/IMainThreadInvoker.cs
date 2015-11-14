#region

using System;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    public interface IMainThreadInvoker : ISingleInstance
    {
        void Invoke(Action action);
    }
}
#region

using System;
using System.Windows.Threading;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    internal class MainThreadInvoker : IMainThreadInvoker
    {
        private readonly Dispatcher dispatcher;

        public MainThreadInvoker()
        {
            dispatcher = Dispatcher.CurrentDispatcher;
        }

        public void Invoke(Action action)
        {
            dispatcher.Invoke(action);
        }
    }
}
using System;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using System.Windows.Threading;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    class MainThreadInvoker : IMainThreadInvoker
    {
        readonly Dispatcher dispatcher;

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

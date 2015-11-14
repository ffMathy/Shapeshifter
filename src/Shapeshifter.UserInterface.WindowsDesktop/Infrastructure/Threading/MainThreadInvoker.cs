using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Threading;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    [ExcludeFromCodeCoverage]
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
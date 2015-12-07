namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Threading;

    using Interfaces;

    [ExcludeFromCodeCoverage]
    class MainThreadInvoker: IMainThreadInvoker
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
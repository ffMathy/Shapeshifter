namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
    using System;
    using System.Windows.Threading;

    using Interfaces;
    
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
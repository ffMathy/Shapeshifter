namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
    using System;
	using System.Threading.Tasks;
	using System.Windows.Threading;

    using Interfaces;
	
	class MainThreadInvoker: IMainThreadInvoker
    {
		readonly Dispatcher dispatcher;

        public MainThreadInvoker()
        {
			dispatcher = Dispatcher.CurrentDispatcher;
            dispatcher.UnhandledException += Dispatcher_UnhandledException;
        }

		static void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			Program.OnGlobalErrorOccured(e.Exception);
		}

        public void Invoke(Action action)
        {
            dispatcher.Invoke(action);
        }

		public T Invoke<T>(Func<T> action)
		{
			return dispatcher.Invoke(action);
        }

		public Task InvokeAsync(Func<Task> action)
		{
			return dispatcher.InvokeAsync(action).Task;
		}
	}
}
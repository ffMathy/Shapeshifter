namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
    using System;
	using System.Threading.Tasks;
	using System.Windows.Threading;

	using Windows.UI.Core;

	using Interfaces;
	
	class MainThreadInvoker: IMainThreadInvoker
    {
		readonly Dispatcher wpfDispatcher;

		public MainThreadInvoker()
        {
			wpfDispatcher = Dispatcher.CurrentDispatcher;
            wpfDispatcher.UnhandledException += WpfDispatcherUnhandledException;
		}

		static void WpfDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			Program.OnGlobalErrorOccured(e.Exception);
		}

        public void Invoke(Action action)
        {
            wpfDispatcher.Invoke(action);
        }

		public T Invoke<T>(Func<T> action)
		{
			return wpfDispatcher.Invoke(action);
        }
	}
}
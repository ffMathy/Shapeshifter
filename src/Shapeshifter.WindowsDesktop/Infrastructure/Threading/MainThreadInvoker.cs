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
        readonly CoreDispatcher uwpDispatcher;

		public MainThreadInvoker()
        {
			wpfDispatcher = Dispatcher.CurrentDispatcher;
            wpfDispatcher.UnhandledException += WpfDispatcherUnhandledException;

			uwpDispatcher = null;
		}

		static void WpfDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			Program.OnGlobalErrorOccured(e.Exception);
		}

        public void InvokeOnWindowsPresentationFoundationThread(Action action)
        {
            wpfDispatcher.Invoke(action);
        }

		public T InvokeOnWindowsPresentationFoundationThread<T>(Func<T> action)
		{
			return wpfDispatcher.Invoke(action);
        }

		public Task InvokeOnUniversalWindowsApplicationThreadAsync(Action action)
		{
			return uwpDispatcher
				.RunAsync(CoreDispatcherPriority.Normal, () => action())
				.AsTask();
		}
	}
}
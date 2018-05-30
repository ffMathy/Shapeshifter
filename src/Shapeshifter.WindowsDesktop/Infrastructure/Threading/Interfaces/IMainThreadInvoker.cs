namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    using System;
	using System.Threading.Tasks;

	using Dependencies.Interfaces;

    public interface IMainThreadInvoker: ISingleInstance
    {
        void InvokeOnWindowsPresentationFoundationThread(Action action);
		T InvokeOnWindowsPresentationFoundationThread<T>(Func<T> action);
		
		Task InvokeOnUniversalWindowsApplicationThreadAsync(Action action);
    }
}
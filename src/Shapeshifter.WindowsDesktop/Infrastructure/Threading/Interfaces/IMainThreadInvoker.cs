namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    using System;
	using System.Threading.Tasks;

	using Dependencies.Interfaces;

    public interface IMainThreadInvoker: ISingleInstance
    {
        void Invoke(Action action);
		T Invoke<T>(Func<T> action);
        Task InvokeAsync(Func<Task> action);
    }
}
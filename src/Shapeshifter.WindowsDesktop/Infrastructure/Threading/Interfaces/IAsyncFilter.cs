namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IAsyncFilter
    {
        Task<IReadOnlyCollection<TResult>> FilterAsync<TResult>(
            IEnumerable<Task<TResult>> candidates,
            Func<TResult, Task<bool>> filter);

        Task<IReadOnlyCollection<TResult>> FilterAsync<TResult>(
            IEnumerable<TResult> candidates,
            Func<TResult, Task<bool>> filter);

        Task<bool> HasMatchAsync<TResult>(
            IEnumerable<Task<TResult>> candidates,
            Func<TResult, Task<bool>> filter);

        Task<bool> HasMatchAsync<TResult>(
            IEnumerable<TResult> candidates,
            Func<TResult, Task<bool>> filter);
    }
}
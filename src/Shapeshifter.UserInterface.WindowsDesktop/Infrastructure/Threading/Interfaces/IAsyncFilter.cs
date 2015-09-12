using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    public interface IAsyncFilter
    {
        Task<IEnumerable<TResult>> FilterAsync<TResult>(IEnumerable<Task<TResult>> candidates, Func<TResult, Task<bool>> filter);

        Task<IEnumerable<TResult>> FilterAsync<TResult>(IEnumerable<TResult> candidates, Func<TResult, Task<bool>> filter);

        Task<IEnumerable<TResult>> FilterAsync<TResult>(IEnumerable<Task<TResult>> candidates, Func<TResult, bool> filter);

        Task<bool> HasMatchAsync<TResult>(IEnumerable<Task<TResult>> candidates, Func<TResult, Task<bool>> filter);

        Task<bool> HasMatchAsync<TResult>(IEnumerable<TResult> candidates, Func<TResult, Task<bool>> filter);

        Task<bool> HasMatchAsync<TResult>(IEnumerable<Task<TResult>> candidates, Func<TResult, bool> filter);
    }
}

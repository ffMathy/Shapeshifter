using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    class AsyncFilter : IAsyncFilter
    {
        public async Task<IEnumerable<TResult>> FilterAsync<TResult>(IEnumerable<Task<TResult>> candidatesTask, Func<TResult, bool> filter)
        {
            return await FilterAsync(candidatesTask, result => Task.FromResult(filter(result))).ConfigureAwait(false);
        }

        public async Task<IEnumerable<TResult>> FilterAsync<TResult>(IEnumerable<TResult> candidatesTask, Func<TResult, Task<bool>> filter)
        {
            return await FilterAsync(candidatesTask.Select(result => Task.FromResult(result)), filter).ConfigureAwait(false);
        }

        public async Task<IEnumerable<TResult>> FilterAsync<TResult>(IEnumerable<Task<TResult>> candidatesTask, Func<TResult, Task<bool>> filter)
        {
            var candidates = await Task.WhenAll(candidatesTask).ConfigureAwait(false);
            var validations = await GetValidationsAsync(filter, candidates).ConfigureAwait(false);

            return FilterUsingValidations(candidates, validations);
        }

        static async Task<bool[]> GetValidationsAsync<TResult>(Func<TResult, Task<bool>> filter, TResult[] candidates)
        {
            var validationsTask = new List<Task<bool>>();
            foreach (var candidate in candidates)
            {
                validationsTask.Add(filter(candidate));
            }

            var validations = await Task.WhenAll(validationsTask).ConfigureAwait(false);
            return validations;
        }

        static IEnumerable<TResult> FilterUsingValidations<TResult>(TResult[] candidates, bool[] validations)
        {
            var results = new List<TResult>();
            for (var i = 0; i < validations.Length; i++)
            {
                if (validations[i])
                {
                    results.Add(candidates[i]);
                }
            }

            return results;
        }
    }
}

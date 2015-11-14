using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    internal class AsyncFilter : IAsyncFilter
    {
        public async Task<IReadOnlyCollection<TResult>> FilterAsync<TResult>(IEnumerable<Task<TResult>> candidatesTask,
            Func<TResult, bool> filter)
        {
            return await FilterAsync(candidatesTask, result => Task.FromResult(filter(result))).ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<TResult>> FilterAsync<TResult>(IEnumerable<TResult> candidatesTask,
            Func<TResult, Task<bool>> filter)
        {
            return await
                    FilterAsync(candidatesTask.Select(Task.FromResult), filter).ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<TResult>> FilterAsync<TResult>(IEnumerable<Task<TResult>> candidatesTask,
            Func<TResult, Task<bool>> filter)
        {
            var candidates = await Task.WhenAll(candidatesTask).ConfigureAwait(false);
            var validations = await GetValidationsAsync(filter, candidates).ConfigureAwait(false);

            return FilterUsingValidations(candidates, validations);
        }

        private static async Task<bool[]> GetValidationsAsync<TResult>(Func<TResult, Task<bool>> filter,
            TResult[] candidates)
        {
            var validationsTask = new List<Task<bool>>();
            foreach (var candidate in candidates)
            {
                validationsTask.Add(filter(candidate));
            }

            var validations = await Task.WhenAll(validationsTask).ConfigureAwait(false);
            return validations;
        }

        private static IReadOnlyCollection<TResult> FilterUsingValidations<TResult>(TResult[] candidates, bool[] validations)
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

        public async Task<bool> HasMatchAsync<TResult>(IEnumerable<Task<TResult>> candidatesTask,
            Func<TResult, bool> filter)
        {
            return await HasMatchAsync(candidatesTask, result => Task.FromResult(filter(result)));
        }

        public async Task<bool> HasMatchAsync<TResult>(IEnumerable<TResult> candidatesTask,
            Func<TResult, Task<bool>> filter)
        {
            return await HasMatchAsync(candidatesTask.Select(Task.FromResult), filter);
        }

        public async Task<bool> HasMatchAsync<TResult>(IEnumerable<Task<TResult>> candidatesTask,
            Func<TResult, Task<bool>> filter)
        {
            //TODO: this can be optimized more by running all tasks in parallel and returning the first succeeding.
            foreach (var candidate in candidatesTask)
            {
                var result = await candidate;
                if (await filter(result))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
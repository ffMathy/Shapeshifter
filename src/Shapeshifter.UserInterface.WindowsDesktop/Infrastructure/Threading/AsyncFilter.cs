namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Interfaces;

    class AsyncFilter: IAsyncFilter
    {
        public async Task<IReadOnlyCollection<TResult>> FilterAsync<TResult>(
            IEnumerable<TResult> candidatesTask,
            Func<TResult, Task<bool>> filter)
        {
            return await
                   FilterAsync(
                       candidatesTask.Select<TResult, Task<TResult>>(Task.FromResult),
                       filter)
                       .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<TResult>> FilterAsync<TResult>(
            IEnumerable<Task<TResult>> candidatesTask,
            Func<TResult, Task<bool>> filter)
        {
            var candidates = await Task.WhenAll(candidatesTask)
                                       .ConfigureAwait(false);
            var validations = await GetValidationsAsync(filter, candidates)
                                        .ConfigureAwait(false);

            return FilterUsingValidations(candidates, validations);
        }

        static async Task<bool[]> GetValidationsAsync<TResult>(
            Func<TResult, Task<bool>> filter,
            TResult[] candidates)
        {
            var validationsTask = new List<Task<bool>>();
            foreach (var candidate in candidates)
            {
                validationsTask.Add(filter(candidate));
            }

            var validations = await Task.WhenAll(validationsTask)
                                        .ConfigureAwait(false);
            return validations;
        }

        static IReadOnlyCollection<TResult> FilterUsingValidations<TResult>(
            IReadOnlyList<TResult> candidates,
            IReadOnlyList<bool> validations)
        {
            var results = new List<TResult>();
            for (var i = 0; i < validations.Count; i++)
            {
                if (validations[i])
                {
                    results.Add(candidates[i]);
                }
            }

            return results;
        }

        public async Task<bool> HasMatchAsync<TResult>(
            IEnumerable<TResult> candidatesTask,
            Func<TResult, Task<bool>> filter)
        {
            return
                await
                HasMatchAsync(
                    candidatesTask.Select<TResult, Task<TResult>>(Task.FromResult),
                    filter);
        }

        public async Task<bool> HasMatchAsync<TResult>(
            IEnumerable<Task<TResult>> candidatesTask,
            Func<TResult, Task<bool>> filter)
        {
            var matchTasks = new List<Task<bool>>();
            foreach (var candidate in candidatesTask)
            {
                matchTasks.Add(IsMatch(filter, candidate));
            }

            while (matchTasks.Count > 0)
            {
                var finishedTask = await Task.WhenAny(matchTasks);
                if (finishedTask.Result)
                {
                    return true;
                }

                matchTasks.Remove(finishedTask);
            }

            return false;
        }

        static async Task<bool> IsMatch<TResult>(
            Func<TResult, Task<bool>> filter,
            Task<TResult> candidate)
        {
            var result = await candidate;
            return await filter(result);
        }
    }
}
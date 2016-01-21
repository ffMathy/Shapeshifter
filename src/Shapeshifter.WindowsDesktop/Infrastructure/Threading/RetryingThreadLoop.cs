namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Interfaces;

    public class RetryingThreadLoop : IRetryingThreadLoop
    {
        readonly IThreadLoop threadLoop;

        public bool IsRunning
            => threadLoop.IsRunning;

        public RetryingThreadLoop(
            IThreadLoop threadLoop)
        {
            this.threadLoop = threadLoop;
        }

        public Task StartAsync(
            RetryingThreadLoopJob job)
        {
            if (job == null)
            {
                throw new ArgumentNullException(
                    nameof(job));
            }

            if (job.Action == null)
            {
                throw new ArgumentNullException(
                    nameof(job.Action));
            }

            if (job.AttemptsBeforeFailing <= 0)
            {
                throw new ArgumentException(
                    "You must provide more than 0 attempts.",
                    nameof(job.AttemptsBeforeFailing));
            }

            return threadLoop.StartAsync(async () => 
                await WrapJobInRetryingMechanism(job),
                job.CancellationToken);
        }

        static async Task WrapJobInRetryingMechanism(
            RetryingThreadLoopJob job)
        {
            var attempts = 0;
            var exceptionsCaught = new HashSet<Exception>();

            try
            {
                attempts++;
                await job.Action();
            }
            catch (Exception ex)
            {
                exceptionsCaught.Add(ex);
                if (!IsExceptionIgnored(job, ex))
                {
                    throw;
                }
                
                if (attempts == job.AttemptsBeforeFailing)
                {
                    throw new AggregateException(
                        "The operation timed out while attempting to invoke the given job, and several exceptions were caught within the duration of these attempts.",
                        exceptionsCaught);
                }

                await Task.Delay(job.IntervalInMilliseconds);
            }
        }

        static bool IsExceptionIgnored(
            RetryingThreadLoopJob job, 
            Exception ex)
        {
            var type = ex.GetType();
            return job
                .IgnoredExceptionTypes
                .Contains(type);
        }

        public void Stop()
        {
            threadLoop.Stop();
        }
    }
}
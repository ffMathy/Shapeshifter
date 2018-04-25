namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Interfaces;
	using Serilog;

	public class RetryingThreadLoop: IRetryingThreadLoop
    {
        readonly IThreadLoop threadLoop;
		readonly ILogger logger;

		public bool IsRunning
            => threadLoop.IsRunning;

        public RetryingThreadLoop(
            IThreadLoop threadLoop,
			ILogger logger)
        {
            this.threadLoop = threadLoop;
			this.logger = logger;
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

            return threadLoop.StartAsync(
                async () =>
                await WrapJobInRetryingMechanism(job),
                job.CancellationToken);
        }

        async Task WrapJobInRetryingMechanism(
            RetryingThreadLoopJob job)
        {
            var attempts = 0;
            var exceptionsCaught = new HashSet<Exception>();

            try
            {
                attempts++;
                await job.Action();
                threadLoop.Stop();
            }
            catch (Exception ex)
            {
				logger.Warning("Attempt #{attempt}. Will try {tries} more times. {message}", attempts, job.AttemptsBeforeFailing - attempts, ex.Message);

                exceptionsCaught.Add(ex);
                if (job.IsExceptionIgnored?.Invoke(ex) != true)
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

        public void Stop()
        {
            threadLoop.Stop();
        }
    }
}
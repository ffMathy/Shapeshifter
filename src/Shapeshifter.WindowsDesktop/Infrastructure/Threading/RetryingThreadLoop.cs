namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
    using System;
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
            return threadLoop.StartAsync(async () => 
                await WrapJobInRetryingMechanism(job),
                job.CancellationToken);
        }

        static async Task WrapJobInRetryingMechanism(
            RetryingThreadLoopJob job)
        {
            try
            {
                await job.Action();
            }
            catch (Exception ex)
            {
                if (!IsExceptionIgnored(job, ex))
                {
                    throw;
                }
            }
        }

        static bool IsExceptionIgnored(
            RetryingThreadLoopJob job, Exception ex)
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
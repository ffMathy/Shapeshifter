namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    using Interfaces;

    using Logging.Interfaces;

    class ConsumerThreadLoop: IConsumerThreadLoop
    {
        readonly IThreadLoop internalLoop;
        readonly ILogger logger;

        int countAvailable;

        public bool IsRunning 
            => internalLoop.IsRunning;

        public ConsumerThreadLoop(
            IThreadLoop internalLoop,
            ILogger logger)
        {
            this.internalLoop = internalLoop;
            this.logger = logger;
        }

        public void Stop()
        {
            lock (this)
            {
                internalLoop.Stop();
                countAvailable = 0;
            }
        }

        public void Notify(Func<Task> action, CancellationToken token)
        {
            lock (this)
            {
                var newCount = Interlocked.Increment(ref countAvailable);
                logger.Information($"Consumer count incremented to {countAvailable}.");

                if ((newCount > 0) && !internalLoop.IsRunning)
                {
                    SpawnThread(action, token);
                }
            }
        }

        void SpawnThread(Func<Task> action, CancellationToken token)
        {
            logger.Information("Spawning consumption thread.");
            internalLoop.StartAsync(async () => await Tick(action, token), token);
        }

        async Task Tick(Func<Task> action, CancellationToken token)
        {
            lock (this)
            {
                if (ShouldAbort(token))
                {
                    logger.Information("Stopping consumer loop.");
                    internalLoop.Stop();
                    return;
                }

                DecrementAvailableWorkCount();
            }

            logger.Information("Consuming item.");
            await action();
        }

        bool ShouldAbort(CancellationToken token)
        {
            Debug.Assert(countAvailable >= 0, "countAvailable >= 0");
            return token.IsCancellationRequested || (countAvailable == 0);
        }

        void DecrementAvailableWorkCount()
        {
            Interlocked.Decrement(ref countAvailable);
            logger.Information($"Consumer count decremented to {countAvailable}.");
        }
    }
}
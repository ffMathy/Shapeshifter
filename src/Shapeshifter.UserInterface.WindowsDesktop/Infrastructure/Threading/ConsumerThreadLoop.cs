using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    internal class ConsumerThreadLoop : IConsumerThreadLoop
    {
        private readonly IThreadLoop internalLoop;
        private readonly ILogger logger;

        private int countAvailable;

        public ConsumerThreadLoop(
            IThreadLoop internalLoop,
            ILogger logger)
        {
            this.internalLoop = internalLoop;
            this.logger = logger;
        }

        public bool IsRunning => internalLoop.IsRunning;

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

                if (newCount > 0 && !internalLoop.IsRunning)
                {
                    SpawnThread(action, token);
                }
            }
        }
        
        private void SpawnThread(Func<Task> action, CancellationToken token)
        {
            logger.Information("Spawning consumption thread.");
            internalLoop.Start(async () => await Tick(action, token), token);
        }
        
        private async Task Tick(Func<Task> action, CancellationToken token)
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

            logger.Information("Consuming.");
            await action();
        }

        private bool ShouldAbort(CancellationToken token)
        {
            Debug.Assert(countAvailable >= 0, "countAvailable >= 0");
            return token.IsCancellationRequested || countAvailable == 0;
        }

        private void DecrementAvailableWorkCount()
        {
            Interlocked.Decrement(ref countAvailable);
            logger.Information($"Consumer count decremented to {countAvailable}.");
        }
    }
}
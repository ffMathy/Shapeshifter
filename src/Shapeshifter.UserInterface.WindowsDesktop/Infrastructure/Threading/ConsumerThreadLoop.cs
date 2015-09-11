using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using System;
using System.Threading;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    class ConsumerThreadLoop : IConsumerThreadLoop
    {
        readonly IThreadLoop internalLoop;
        readonly ILogger logger;

        int countAvailable;

        public ConsumerThreadLoop(
            IThreadLoop internalLoop,
            ILogger logger)
        {
            this.internalLoop = internalLoop;
            this.logger = logger;
        }

        public bool IsRunning
        {
            get
            {
                return internalLoop.IsRunning;
            }
        }

        public void Stop()
        {
            internalLoop.Stop();
            countAvailable = 0;
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

        void SpawnThread(Func<Task> action, CancellationToken token)
        {
            internalLoop.StartAsync(async () =>
            {
                lock (this)
                {
                    if (ShouldAbort(token))
                    {
                        internalLoop.Stop();
                        return;
                    }

                    DecrementAvailableWorkCount();
                }

                await action();
            }, token);
        }

        private bool ShouldAbort(CancellationToken token)
        {
            return token.IsCancellationRequested || !IsRunning || countAvailable == 0;
        }

        void DecrementAvailableWorkCount()
        {
            Interlocked.Decrement(ref countAvailable);
            logger.Information($"Consumer count decremented to {countAvailable}.");
        }
    }
}

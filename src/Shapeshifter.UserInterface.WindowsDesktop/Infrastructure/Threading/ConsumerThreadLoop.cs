#region

using System;
using System.Threading;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;

#endregion

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

        public bool IsRunning
        {
            get { return internalLoop.IsRunning; }
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

        private void SpawnThread(Func<Task> action, CancellationToken token)
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

        private void DecrementAvailableWorkCount()
        {
            Interlocked.Decrement(ref countAvailable);
            logger.Information($"Consumer count decremented to {countAvailable}.");
        }
    }
}
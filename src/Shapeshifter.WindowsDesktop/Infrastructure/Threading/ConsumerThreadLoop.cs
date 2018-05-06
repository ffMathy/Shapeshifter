namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	using Interfaces;
	using Serilog;

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
                if ((newCount > 0) && !internalLoop.IsRunning)
                {
                    SpawnThread(action, token);
                }
            }
        }

        void SpawnThread(Func<Task> action, CancellationToken token)
        {
            internalLoop.StartAsync(async () => await Tick(action, token), token);
        }

        async Task Tick(Func<Task> action, CancellationToken token)
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
        }

        bool ShouldAbort(CancellationToken token)
        {
            return token.IsCancellationRequested || (countAvailable == 0);
        }

        void DecrementAvailableWorkCount()
        {
            Interlocked.Decrement(ref countAvailable);
        }
    }
}
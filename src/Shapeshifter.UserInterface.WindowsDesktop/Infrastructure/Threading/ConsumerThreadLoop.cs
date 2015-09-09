using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using System;
using System.Threading;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    class ConsumerThreadLoop : IConsumerThreadLoop
    {
        readonly IThreadLoop internalLoop;
        readonly ILogger logger;

        readonly ManualResetEventSlim dataReadyEvent;

        int countAvailable;

        public ConsumerThreadLoop(
            IThreadLoop internalLoop,
            ILogger logger)
        {
            this.internalLoop = internalLoop;
            this.logger = logger;

            dataReadyEvent = new ManualResetEventSlim();
        }

        public bool IsRunning
        {
            get
            {
                return internalLoop.IsRunning;
            }
        }

        public void Start(Action action, CancellationToken token)
        {
            internalLoop.Start(() =>
            {
                dataReadyEvent.Wait(token);

                if (token.IsCancellationRequested || !IsRunning)
                {
                    return;
                }
                
                if (countAvailable > 0)
                {
                    Interlocked.Decrement(ref countAvailable);
                    if(countAvailable == 0)
                    {
                        dataReadyEvent.Reset();
                    }

                    logger.Information($"Consumer count decremented to {countAvailable}.");
                    action();
                }
            }, token);
        }

        public void Stop()
        {
            internalLoop.Stop();
            countAvailable = 0;
        }

        public void Notify()
        {
            Interlocked.Increment(ref countAvailable);
            logger.Information($"Consumer count incremented to {countAvailable}.");
            dataReadyEvent.Set();
        }
    }
}

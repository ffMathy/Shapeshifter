using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    class ConsumerThreadLoop : IConsumerThreadLoop
    {
        readonly IThreadLoop internalLoop;

        readonly ManualResetEventSlim dataReadyEvent;

        int countAvailable;

        public ConsumerThreadLoop(IThreadLoop internalLoop)
        {
            this.internalLoop = internalLoop;

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
                dataReadyEvent.Reset();

                if(token.IsCancellationRequested || !IsRunning)
                {
                    return;
                }

                if (countAvailable > 0)
                {
                    Interlocked.Decrement(ref countAvailable);

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
            dataReadyEvent.Set();
        }
    }
}

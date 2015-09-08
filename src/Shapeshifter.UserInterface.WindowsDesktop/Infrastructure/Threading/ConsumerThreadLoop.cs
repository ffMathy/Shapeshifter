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

        readonly CountdownEvent dataReadySemaphore;

        public ConsumerThreadLoop(IThreadLoop internalLoop)
        {
            this.internalLoop = internalLoop;

            dataReadySemaphore = new CountdownEvent(0);
        }

        public bool IsRunning
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Start(Action action, CancellationToken token)
        {
            internalLoop.Start(() =>
            {
                dataReadySemaphore.Signal();
                action();
            }, token);
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void Notify()
        {
            dataReadySemaphore.AddCount(1);
        }
    }
}

namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Interfaces;

    using Shared.Infrastructure.Logging.Interfaces;

    class ThreadLoop: IThreadLoop
    {
        readonly ILogger logger;

        public ThreadLoop(ILogger logger)
        {
            this.logger = logger;
        }

        public bool IsRunning { get; private set; }

        public async Task StartAsync(Func<Task> action, CancellationToken token)
        {
            lock (this)
            {
                if (IsRunning)
                {
                    throw new InvalidOperationException("Can't start the thread loop twice.");
                }

                IsRunning = true;
            }

            using (logger.Indent())
            {
                await RunAsync(action, token);
            }
        }

        async Task RunAsync(Func<Task> action, CancellationToken token = default(CancellationToken))
        {
            while (!token.IsCancellationRequested && IsRunning)
            {
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    logger.Error("An error occured in the thread loop: " + ex);
                    Stop();
                    throw;
                }
            }

            Stop();
        }

        public void Stop()
        {
            lock (this)
            {
                IsRunning = false;
            }
        }
    }
}
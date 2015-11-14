#region

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    internal interface IThreadLoop
    {
        bool IsRunning { get; }

        void StartAsync(Func<Task> action, CancellationToken token);

        void Stop();
    }
}
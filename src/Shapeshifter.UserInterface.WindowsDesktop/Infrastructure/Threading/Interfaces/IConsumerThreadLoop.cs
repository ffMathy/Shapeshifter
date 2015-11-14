﻿#region

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    internal interface IConsumerThreadLoop
    {
        bool IsRunning { get; }

        void Notify(Func<Task> action, CancellationToken token);

        void Stop();
    }
}
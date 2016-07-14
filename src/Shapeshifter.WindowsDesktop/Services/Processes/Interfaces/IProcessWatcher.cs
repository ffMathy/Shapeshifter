using Shapeshifter.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.WindowsDesktop.Services.Interfaces;
using System;
using System.Collections.Generic;

namespace Shapeshifter.WindowsDesktop.Services.Processes.Interfaces
{
    public interface IProcessWatcher: IHookService
    {
        event EventHandler<ProcessStartedEventArgument> ProcessStarted;

        IReadOnlyList<string> ProcessNamesToWatch { get; }

        void AddProcessNameToWatchList(string processName);
        void RemoveProcessNameFromWatchList(string processName);
    }
}

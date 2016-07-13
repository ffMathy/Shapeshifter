using Shapeshifter.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.WindowsDesktop.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Services.Processes.Interfaces
{
    interface IProcessWatcher: IHookService
    {
        event EventHandler<ProcessStartedEventArgument> ProcessStarted;
    }
}

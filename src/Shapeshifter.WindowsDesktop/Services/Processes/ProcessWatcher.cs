using Shapeshifter.WindowsDesktop.Services.Processes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shapeshifter.WindowsDesktop.Infrastructure.Events;

namespace Shapeshifter.WindowsDesktop.Services.Processes
{
    class ProcessWatcher : IProcessWatcher
    {
        public event EventHandler<ProcessStartedEventArgument> ProcessStarted;
    }
}

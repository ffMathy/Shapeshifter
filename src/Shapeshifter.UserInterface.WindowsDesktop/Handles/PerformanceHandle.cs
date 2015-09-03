using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Handles
{
    class PerformanceHandle : IPerformanceHandle
    {
        readonly ILogger logger;

        readonly string methodName;
        readonly DateTime startTime;

        public PerformanceHandle(ILogger logger, string methodName)
        {
            this.logger = logger;
            this.methodName = methodName;

            startTime = DateTime.UtcNow;

            logger.Performance("Started executing " + methodName + ".");
        }

        public void Dispose()
        {
            var now = DateTime.UtcNow;
            logger.Performance("Finished executing " + methodName + " in " + (now - startTime).TotalMilliseconds + " milliseconds.");
        }
    }
}

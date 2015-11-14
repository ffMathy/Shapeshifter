﻿#region

using System.Runtime.CompilerServices;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories
{
    internal class PerformanceHandleFactory : IPerformanceHandleFactory
    {
        private readonly ILogger logger;

        public PerformanceHandleFactory(ILogger logger)
        {
            this.logger = logger;
        }

        public IPerformanceHandle StartMeasuringPerformance([CallerMemberName] string methodName = "")
        {
            return new PerformanceHandle(logger, methodName);
        }
    }
}
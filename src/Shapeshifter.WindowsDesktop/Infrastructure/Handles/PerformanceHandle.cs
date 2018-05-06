namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles
{
	using System;

	using Interfaces;
	using Serilog;

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
		}

		public void Dispose()
		{
			var now = DateTime.UtcNow;
			var time = (now - startTime).TotalMilliseconds;
			if (time < 100)
				return;

			logger.Verbose(
				"Finished executing " + methodName + " in " +
				(now - startTime).TotalMilliseconds +
				" milliseconds.");
		}
	}
}
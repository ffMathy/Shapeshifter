using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging
{
	class IssueReporterSink : ILogEventSink
	{
		readonly Stack<string> logHistory;
		readonly Queue<LogEvent> pendingLogEvents;

		const int logHistoryLength = 100;

		public IssueReporterSink()
		{
			logHistory = new Stack<string>();
		}

		void ScheduleLogEventReport(LogEvent logEvent)
		{
			try
			{
				lock (logHistory)
				{
					var lastMessages = new List<string>();
					for (var i = 0; i < logHistoryLength && logHistory.Count > 0; i++)
					{
						lastMessages.Add(logHistory.Pop());
					}

					ReportLogEvent(logEvent, lastMessages);
				}
			}
			catch
			{
				//don't allow errors here to ruin everything.
			}
		}

		async void ReportLogEvent(LogEvent logEvent, List<string> lastMessages)
		{
			var client = new FluffySpoon
		}

		public void Emit(LogEvent logEvent)
		{
			if (logEvent.Exception != null || logEvent.Level == LogEventLevel.Error || logEvent.Level == LogEventLevel.Warning)
				ScheduleLogEventReport(logEvent);

			var message = logEvent.RenderMessage();
			lock(logHistory) { 
				logHistory.Push(message);
			}
		}
	}
}

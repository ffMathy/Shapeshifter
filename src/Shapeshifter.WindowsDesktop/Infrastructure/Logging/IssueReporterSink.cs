using FluffySpoon.Http;
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
		readonly IRestClient restClient;

		const int logHistoryLength = 100;

		public IssueReporterSink()
		{
			logHistory = new Stack<string>();
			restClient = new RestClient();
		}

		void ScheduleLogEventReport(LogEvent logEvent)
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

		async void ReportLogEvent(LogEvent logEvent, List<string> lastMessages)
		{
			try
			{
				//await restClient.PostAsync<string>(
				//	new Uri("https://shapeshifter.azurewebsites.net/api/github/report"))
			}
			catch
			{
				//don't allow errors here to ruin everything.
			}
		}

		public void Emit(LogEvent logEvent)
		{
			if (logEvent.Exception != null || logEvent.Level == LogEventLevel.Error || logEvent.Level == LogEventLevel.Warning)
				ScheduleLogEventReport(logEvent);

			var message = logEvent.RenderMessage();
			lock (logHistory)
			{
				logHistory.Push(message);
			}
		}
	}
}

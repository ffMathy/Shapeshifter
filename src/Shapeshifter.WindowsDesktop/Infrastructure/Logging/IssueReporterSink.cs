using FluffySpoon.Http;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Shapeshifter.Website.Models.GitHub.Request;
using Shapeshifter.WindowsDesktop.Shared.GitHub.Response;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging
{
	class IssueReporterSink : ILogEventSink
	{
		readonly Stack<string> logHistory;
		readonly IRestClient restClient;

		readonly SemaphoreSlim reportingSemaphore;

		const int logHistoryLength = 100;

		public IssueReporterSink()
		{
			logHistory = new Stack<string>();
			restClient = new RestClient();
			reportingSemaphore = new SemaphoreSlim(1);
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

		async void ReportLogEvent(LogEvent logEvent, IEnumerable<string> lastMessages)
		{
			try
			{
				await reportingSemaphore.WaitAsync();

				var contextBuilder = new StringBuilder();

				using (var writer = new StringWriter(contextBuilder))
					logEvent.Properties.SingleOrDefault(x => x.Key == "SourceContext").Value?.Render(writer);

				var context = contextBuilder.ToString();
				context = context?.Trim('\"') ?? "";

				var issueReport = new IssueReport() {
					Exception = ConvertExceptionToSerializableException(logEvent),
					OffendingLogLine = logEvent.RenderMessage(),
					OffendingLogMessage = logEvent.MessageTemplate.Text,
					RecentLogLines = lastMessages.ToArray(),
					Version = Program.GetCurrentVersion().ToString(),
					Context = context
				};

				var response = await restClient.PostAsync<ReportResponse>(
					new Uri("https://shapeshifter.azurewebsites.net/api/github/report"),
					issueReport,
					new Dictionary<HttpRequestHeader, string>());

				Log.Logger.Verbose("Reported the log entry {entryName} as {githubIssueLink}.", logEvent.MessageTemplate.Text, response.IssueUrl);
			}
			catch
			{
				//don't allow errors here to ruin everything.
			}
			finally {
				reportingSemaphore.Release();
			}
		}

		private SerializableException ConvertExceptionToSerializableException(LogEvent logEvent)
		{
			if(logEvent.Exception == null)
				return null;

			return new SerializableException() {
				Message = logEvent.Exception.Message,
				Name = logEvent.Exception.GetType().Name,
				StackTrace = logEvent.Exception.StackTrace
			};
		}

		public void Emit(LogEvent logEvent)
		{
			if (logEvent.Level == LogEventLevel.Error || logEvent.Level == LogEventLevel.Warning)
				ScheduleLogEventReport(logEvent);

			var message = logEvent.RenderMessage();
			lock (logHistory)
			{
				logHistory.Push(message);
			}
		}
	}
}

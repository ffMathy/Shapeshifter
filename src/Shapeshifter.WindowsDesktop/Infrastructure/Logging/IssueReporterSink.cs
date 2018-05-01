using FluffySpoon.Http;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Shapeshifter.Website.Models.GitHub.Request;
using Shapeshifter.WindowsDesktop.Infrastructure.Environment.Interfaces;
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
		readonly SemaphoreSlim reportingSemaphore;

		readonly IRestClient restClient;
		readonly IEnvironmentInformation environmentInformation;

		const int logHistoryLength = 100;

		public IssueReporterSink(IEnvironmentInformation environmentInformation)
		{
			logHistory = new Stack<string>();
			restClient = new RestClient();
			reportingSemaphore = new SemaphoreSlim(1);

			this.environmentInformation = environmentInformation;
		}

		void ScheduleLogEventReport(LogEvent logEvent)
		{
			lock (logHistory)
			{
				Log.Logger.Verbose("Reporting the log entry \"{entryName}\".", logEvent.MessageTemplate.Text);

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
			if (environmentInformation.GetIsDebugging())
				return;

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

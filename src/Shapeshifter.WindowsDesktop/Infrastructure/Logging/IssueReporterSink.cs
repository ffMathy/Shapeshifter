using FluffySpoon.Http;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Shapeshifter.Website.Models.GitHub.Request;
using Shapeshifter.WindowsDesktop.Infrastructure.Environment.Interfaces;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using static System.Environment;
using SystemEnvironment = System.Environment;

namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging
{
	using Website.Models.GitHub.Response;

	class IssueReporterSink : ILogEventSink
	{
		readonly LinkedList<string> logHistory;
		readonly SemaphoreSlim reportingSemaphore;

		readonly IRestClient restClient;
		readonly IEnvironmentInformation environmentInformation;

		const int logHistoryLength = 1000;

		public IssueReporterSink(IEnvironmentInformation environmentInformation)
		{
			logHistory = new LinkedList<string>();
			restClient = new RestClient();
			reportingSemaphore = new SemaphoreSlim(1);

			this.environmentInformation = environmentInformation;
		}

		void ScheduleLogEventReport(LogEvent logEvent)
		{
			lock (logHistory)
			{
				Log.Logger.Verbose("Reporting the log entry \"{entryName}\".", logEvent.MessageTemplate.Text);
				ReportLogEvent(logEvent, logHistory);
			}
		}

		async void ReportLogEvent(LogEvent logEvent, IEnumerable<string> lastMessages)
		{
			try
			{
				await reportingSemaphore.WaitAsync();

				var context = GetPropertyValue(logEvent, "SourceContext");
				context = context?.Trim('\"') ?? "";

				var issueReport = new IssueReport() {
					Exception = ConvertExceptionToSerializableException(logEvent),
					OffendingLogLine = StripSensitiveInformation(logEvent.RenderMessage()),
					OffendingLogMessage = StripSensitiveInformation(logEvent.MessageTemplate.Text),
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

		static string StripSensitiveInformation(string input)
		{
			return input
				.Replace(
					GetFolderPath(SpecialFolder.UserProfile),
					"%USERPROFILE%")
				.Replace(
					UserName, 
					"%USERNAME%");
		}

		static string GetPropertyValue(LogEvent logEvent, string name)
		{
			var contextBuilder = new StringBuilder();

			using (var writer = new StringWriter(contextBuilder))
				logEvent.Properties.SingleOrDefault(x => x.Key == name).Value?.Render(writer);

			var context = contextBuilder.ToString();
			return context;
		}

		static SerializableException ConvertExceptionToSerializableException(LogEvent logEvent)
		{
			if(logEvent.Exception == null)
				return null;

			return new SerializableException() {
				Message = StripSensitiveInformation(logEvent.Exception.Message),
				Name = logEvent.Exception.GetType().Name,
				StackTrace = StripSensitiveInformation(logEvent.Exception.StackTrace)
			};
		}

		public void Emit(LogEvent logEvent)
		{
			if (environmentInformation.GetIsDebugging())
				return;

			if (logEvent.Level == LogEventLevel.Error || logEvent.Level == LogEventLevel.Warning)
				ScheduleLogEventReport(logEvent);

			var level = GetPropertyValue(logEvent, "Level");
			var message = StripSensitiveInformation($"{level} {logEvent.RenderMessage()}");
			lock (logHistory)
			{
				logHistory.AddLast(message);
				if(logHistory.Count > logHistoryLength)
					logHistory.RemoveFirst();
			}
		}
	}
}

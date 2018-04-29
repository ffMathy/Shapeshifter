using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using Shapeshifter.Website.Models;
using Shapeshifter.Website.Models.GitHub.Request;

namespace Shapeshifter.Website.Controllers
{
	[Produces("application/json")]
	[Route("api/github")]
	public class GitHubApiController : Controller
	{
		private readonly IGitHubClient _client;

		private static SemaphoreSlim _reportLock;

		static GitHubApiController() {
			_reportLock = new SemaphoreSlim(1);
		}

		public GitHubApiController(
		  IConfigurationReader configurationReader)
		{
			var client = new GitHubClient(new ProductHeaderValue("Shapeshifter.Website"));
			client.Credentials = new Credentials(configurationReader.Read("github.token"));

			_client = client;
		}

		[HttpPost("report")]
		public async Task<string> ReportIssue([FromBody] IssueReport issueReport)
		{
			await _reportLock.WaitAsync();

			try
			{
				const string username = "ffMathy";
				const string repository = "Shapeshifter";

				var existingIssues = await _client.Issue.GetAllForRepository(
					username,
					repository,
					new RepositoryIssueRequest() {
						Filter = IssueFilter.Created
					});

				string issueTitle;
				if (issueReport.Exception != null)
				{
					issueTitle = issueReport.Exception.Name + " occured in " + issueReport.Exception.Context;
				}
				else
				{
					issueTitle = issueReport.OffendingLogMessage;
				}

				issueTitle = issueTitle.TrimEnd('.', ' ', '\n', '\r');

				var existingIssue = existingIssues
					.Where(x => x.Title == issueTitle)
					.FirstOrDefault(x =>
						x.State.Value == ItemState.Open ||
						DateTime.UtcNow - x.CreatedAt >= TimeSpan.FromDays(3));
				if (existingIssue == null)
				{
					existingIssue = await _client.Issue.Create(
						username,
						repository,
						new NewIssue(issueTitle) {
							Body = RenderIssueBody(issueReport)
						});
				}

				return existingIssue.HtmlUrl;
			} finally {
				_reportLock.Release();
			}
		}

		string RenderIssueBody(IssueReport issueReport)
		{
			var body = string.Empty;

			body += $"<b>Version:</b> {issueReport.Version}\n\n";

			if (issueReport.Exception != null)
			{
				body += "<h1>Exception</h1>\n";
				body += $"<b>Type:</b> {issueReport.Exception.Name}\n\n";
				body += $"<b>Offending class:</b> {issueReport.Exception.Context}\n\n";
				body += $"```\n{issueReport.Exception.StackTrace}\n```\n\n";
			}

			if (issueReport.RecentLogLines != null && issueReport.RecentLogLines.Length > 0)
			{
				body += "<h1>Log</h1>\n";
				if (issueReport.OffendingLogLine != null)
				{
					foreach (var line in issueReport.OffendingLogLine.Split('\n', '\r'))
					{
						if (string.IsNullOrEmpty(line.Trim()))
							continue;

						body += $"> {line}\n";
					}
					body += "\n";
				}

				foreach (var line in issueReport.RecentLogLines)
				{
					if (string.IsNullOrEmpty(line.Trim()))
						continue;

					body += $"> {line}\n";
				}
			}

			return body;
		}
	}
}

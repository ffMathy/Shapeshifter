using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Octokit;

using Shapeshifter.Website.Models.GitHub.Request;

namespace Shapeshifter.Website.Controllers
{
	using Logic;

	using Models.GitHub.Response;

	[Produces("application/json")]
	[Route("api/github")]
	public class GitHubApiController : Controller
	{
		readonly IGitHubClient _client;

		static SemaphoreSlim _reportLock;

		static GitHubApiController() {
			_reportLock = new SemaphoreSlim(1);
		}

		public GitHubApiController(
		  IConfigurationReader configurationReader)
		{
			_client = new GitHubClient(new ProductHeaderValue("Shapeshifter.Website"))
			{
				Credentials = new Credentials(configurationReader.Read("github.token"))
			};
		}

		[HttpPost("report")]
		public async Task<ReportResponse> ReportIssue([FromBody] IssueReport issueReport)
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
						State = ItemState.All,
						Filter = IssueFilter.Created
					});

				var issueTitle = issueReport.Exception != null ? 
					issueReport.Exception.Name : 
					issueReport.OffendingLogMessage;

				issueTitle = issueTitle.TrimEnd('.', ' ', '\n', '\r');

				if(!string.IsNullOrWhiteSpace(issueReport.Context)) {
					var nameSplit = issueReport.Context.Split('.');
					issueReport.Context = nameSplit[nameSplit.Length - 1];
					
					issueTitle += " in " + issueReport.Context;
				}
				
				issueTitle += " on v" + issueReport.Version;

				var existingIssue = existingIssues.FirstOrDefault(x => x.Title == issueTitle) ?? 
					await _client.Issue.Create(
						username,
						repository,
						new NewIssue(issueTitle) {
							Body = RenderIssueBody(issueReport)
						});

				return new ReportResponse() {
					IssueUrl = existingIssue.HtmlUrl
				};
			} finally {
				_reportLock.Release();
			}
		}

		static string RenderIssueBody(IssueReport issueReport)
		{
			var body = string.Empty;

			body += $"<b>Version:</b> {issueReport.Version}\n\n";
			
			if(!string.IsNullOrEmpty(issueReport.Context))
				body += $"<b>Offending class:</b> {issueReport.Context}\n\n";

			if (issueReport.Exception != null)
			{
				body += "<h1>Exception</h1>\n";
				body += $"<b>Type:</b> {issueReport.Exception.Name}\n\n";
				body += $"<b>Message:</b> {issueReport.Exception.Message}\n\n";
				body += $"```\n{issueReport.Exception.StackTrace}\n```\n\n";
			}

			if (issueReport.RecentLogLines == null || issueReport.RecentLogLines.Length <= 0) 
				return body;

			body += "<h1>Log</h1>\n\n";
			if (issueReport.OffendingLogLine != null)
			{
				body += ConvertLinesIntoCodeRegion(
					issueReport.OffendingLogLine.Split('\n', '\r'));
			}

			body += "<details><summary>Full log</summary><p>\n\n";
			body += ConvertLinesIntoCodeRegion(issueReport.RecentLogLines);
			body += "\n</p></details>";

			return body;
		}

		static string ConvertLinesIntoCodeRegion(string[] lines)
		{
			var body = "```\n";
			foreach (var line in lines)
			{
				if (string.IsNullOrEmpty(line.Trim()))
					continue;

				body += $"{line}\n";
			}
			body += "```\n";
			return body;
		}
	}
}

namespace Shapeshifter.Website.Controllers;

using System.Text;

using Logic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Models.GitHub.Request;
using Models.GitHub.Response;

using Octokit;

[ApiController]
[Produces("application/json")]
[Route("api/github")]
public sealed class GitHubApiController(
	IGitHubClient client,
	IOptions<GitHubOptions> options) : ControllerBase
{
	// Serializes reports so that two identical crashes reported at once do not create duplicate issues.
	static readonly SemaphoreSlim ReportLock = new(1, 1);

	[HttpPost("report")]
	public async Task<ActionResult<ReportResponse>> ReportIssue(
		[FromBody] IssueReport issueReport,
		CancellationToken cancellationToken)
	{
		var settings = options.Value;
		if (string.IsNullOrWhiteSpace(settings.Token))
		{
			return Problem(
				statusCode: StatusCodes.Status503ServiceUnavailable,
				detail: "Issue reporting is not configured on this server.");
		}

		var issueTitle = BuildIssueTitle(issueReport, out var context);
		if (issueTitle == null)
			return BadRequest("The report must contain an exception name or an offending log message.");

		await ReportLock.WaitAsync(cancellationToken);
		try
		{
			var existingIssues = await client.Issue.GetAllForRepository(
				settings.Owner,
				settings.Repository,
				new RepositoryIssueRequest
				{
					State = ItemStateFilter.All,
					Filter = IssueFilter.Created
				});

			var existingIssue = existingIssues.FirstOrDefault(x => x.Title == issueTitle) ??
				await client.Issue.Create(
					settings.Owner,
					settings.Repository,
					new NewIssue(issueTitle)
					{
						Body = RenderIssueBody(issueReport, context)
					});

			return new ReportResponse
			{
				IssueUrl = existingIssue.HtmlUrl
			};
		}
		finally
		{
			ReportLock.Release();
		}
	}

	static string? BuildIssueTitle(IssueReport issueReport, out string? context)
	{
		context = null;

		var issueTitle = issueReport.Exception?.Name ?? issueReport.OffendingLogMessage;
		if (string.IsNullOrWhiteSpace(issueTitle))
			return null;

		issueTitle = issueTitle.TrimEnd('.', ' ', '\n', '\r');

		if (!string.IsNullOrWhiteSpace(issueReport.Context))
		{
			// Only the class name, not the full namespace.
			context = issueReport.Context.Split('.')[^1];
			issueTitle += " in " + context;
		}

		return issueTitle + " on v" + issueReport.Version;
	}

	static string RenderIssueBody(IssueReport issueReport, string? context)
	{
		var body = new StringBuilder();

		body.Append("<b>Version:</b> ").Append(issueReport.Version).Append("\n\n");

		if (!string.IsNullOrEmpty(context))
			body.Append("<b>Offending class:</b> ").Append(context).Append("\n\n");

		if (issueReport.Exception != null)
		{
			body.Append("<h1>Exception</h1>\n");
			body.Append("<b>Type:</b> ").Append(issueReport.Exception.Name).Append("\n\n");
			body.Append("<b>Message:</b> ").Append(issueReport.Exception.Message).Append("\n\n");
			body.Append("```\n").Append(issueReport.Exception.StackTrace).Append("\n```\n\n");
		}

		if (issueReport.RecentLogLines == null || issueReport.RecentLogLines.Length <= 0)
			return body.ToString();

		body.Append("<h1>Log</h1>\n\n");
		if (issueReport.OffendingLogLine != null)
			AppendCodeRegion(body, issueReport.OffendingLogLine.Split('\n', '\r'));

		body.Append("<details><summary>Full log</summary><p>\n\n");
		AppendCodeRegion(body, issueReport.RecentLogLines);
		body.Append("\n</p></details>");

		return body.ToString();
	}

	static void AppendCodeRegion(StringBuilder body, string?[] lines)
	{
		body.Append("```\n");
		foreach (var line in lines)
		{
			if (string.IsNullOrWhiteSpace(line))
				continue;

			body.Append(line).Append('\n');
		}
		body.Append("```\n");
	}
}

using System.Runtime.InteropServices;
using System.Text;
using Shapeshifter.Core.Updates;

namespace Shapeshifter.Core.Support;

/// <summary>
/// What a bug report needs to say about the running copy, and a link that opens a new GitHub issue with it filled in
/// (issues #342 and #561), so nobody has to dig out the version number by hand.
/// </summary>
public static class ProblemReport
{
    /// <summary>
    /// "Shapeshifter 7.1.0 · Linux (Ubuntu 24.04.1 LTS) · X64 · X11": the version, the operating system, the processor
    /// architecture and, on Linux, the display server.
    /// </summary>
    public static string DescribeEnvironment(string appName, string version, string operatingSystem, Architecture architecture, string? displayServer)
    {
        var parts = new List<string> { $"{appName} {version}", operatingSystem, architecture.ToString() };
        if (!string.IsNullOrWhiteSpace(displayServer))
        {
            parts.Add(displayServer);
        }

        return string.Join(" · ", parts);
    }

    /// <summary><see cref="DescribeEnvironment(string, string, string, Architecture, string?)"/> for this process.</summary>
    public static string DescribeCurrentEnvironment(string appName) => DescribeEnvironment(
        appName,
        ReleaseVersion.Current().ToString(),
        RuntimeInformation.OSDescription.Trim(),
        RuntimeInformation.OSArchitecture,
        OperatingSystem.IsLinux() ? CurrentDisplayServer() : null);

    /// <summary>The page that opens a new issue on GitHub with the environment and the usual questions filled in.</summary>
    public static Uri CreateIssueUrl(AppIdentity app, string environment)
    {
        ArgumentNullException.ThrowIfNull(app);
        var body = new StringBuilder()
            .AppendLine("**What happened?**")
            .AppendLine()
            .AppendLine()
            .AppendLine("**What did you expect to happen?**")
            .AppendLine()
            .AppendLine()
            .AppendLine("**Steps to reproduce** (which application you copied from and pasted into)")
            .AppendLine()
            .AppendLine("1. ")
            .AppendLine()
            .AppendLine("**Environment**")
            .AppendLine()
            .AppendLine(environment)
            .AppendLine()
            .AppendLine("<!-- If something went wrong, attach the log file (\"Open log folder\" in Shapeshifter's menu). Remove anything private from it first: it can contain window titles. -->")
            .ToString();
        return new Uri(
            $"https://github.com/{app.GitHubOwner}/{app.GitHubRepository}/issues/new?labels=bug&body={Uri.EscapeDataString(body)}");
    }

    private static string? CurrentDisplayServer()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
        {
            return "Wayland";
        }

        return string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) ? null : "X11";
    }
}

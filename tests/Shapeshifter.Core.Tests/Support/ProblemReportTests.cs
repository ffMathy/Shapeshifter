using System.Runtime.InteropServices;
using Shapeshifter.Core.Support;
using Shapeshifter.Core.Tests.Updates;

namespace Shapeshifter.Core.Tests.Support;

public sealed class ProblemReportTests
{
    [Fact]
    public void DescribeEnvironment_ListsVersionSystemArchitectureAndDisplayServer()
    {
        Assert.Equal(
            "Shapeshifter 7.1.0 · Ubuntu 24.04 · X64 · X11",
            ProblemReport.DescribeEnvironment("Shapeshifter", "7.1.0", "Ubuntu 24.04", Architecture.X64, "X11"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DescribeEnvironment_LeavesOutAnUnknownDisplayServer(string? displayServer)
    {
        Assert.Equal(
            "Shapeshifter 7.1.0 · Windows 11 · Arm64",
            ProblemReport.DescribeEnvironment("Shapeshifter", "7.1.0", "Windows 11", Architecture.Arm64, displayServer));
    }

    [Fact]
    public void DescribeCurrentEnvironment_StartsWithTheNameAndVersion()
    {
        Assert.StartsWith("Shapeshifter ", ProblemReport.DescribeCurrentEnvironment("Shapeshifter"), StringComparison.Ordinal);
    }

    [Fact]
    public void CreateIssueUrl_OpensANewBugInTheRepository_WithTheEnvironmentFilledIn()
    {
        var url = ProblemReport.CreateIssueUrl(TestApp.Identity, "Shapeshifter 7.1.0 · Windows 11 & more");

        Assert.Equal("github.com", url.Host);
        Assert.Equal($"/{TestApp.Identity.GitHubOwner}/{TestApp.Identity.GitHubRepository}/issues/new", url.AbsolutePath);
        var query = url.Query.TrimStart('?').Split('&').Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => Uri.UnescapeDataString(pair[1]));
        Assert.Equal("bug", query["labels"]);
        Assert.Contains("Shapeshifter 7.1.0 · Windows 11 & more", query["body"], StringComparison.Ordinal);
        Assert.Contains("**What happened?**", query["body"], StringComparison.Ordinal);
    }
}

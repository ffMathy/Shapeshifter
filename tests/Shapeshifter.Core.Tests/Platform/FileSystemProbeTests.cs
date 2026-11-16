using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Tests.Platform;

public sealed class FileSystemProbeTests : IDisposable
{
    private readonly DirectoryInfo root = Directory.CreateTempSubdirectory("shapeshifter-probe-");

    public void Dispose() => root.Delete(recursive: true);

    [Theory]
    [InlineData(@"\\server\share\folder")]
    [InlineData("//server/share/folder")]
    public void UncPaths_AreNetworkPaths(string path) => Assert.True(FileSystemProbe.IsNetworkPath(path));

    [Theory]
    [InlineData(@"\\server\share\Reports", true)]
    [InlineData(@"\\server\share\report.pdf", false)]
    [InlineData("//server/share/Reports", true)]
    [InlineData("//server/share/report.pdf", false)]
    public void NetworkPaths_AreJudgedByTheirExtension(string path, bool isDirectory)
    {
        Assert.Equal(isDirectory, FileSystemProbe.IsDirectory(path));
        Assert.False(FileSystemProbe.IsLocalFile(path));
    }

    [Fact]
    public void LocalPaths_AreChecked()
    {
        var folder = root.CreateSubdirectory("folder.with.dots").FullName;
        var file = Path.Combine(root.FullName, "noextension");
        File.WriteAllText(file, "x");

        Assert.False(FileSystemProbe.IsNetworkPath(folder));
        Assert.True(FileSystemProbe.IsDirectory(folder));
        Assert.False(FileSystemProbe.IsDirectory(file));
        Assert.True(FileSystemProbe.IsLocalFile(file));
        Assert.False(FileSystemProbe.IsLocalFile(folder));
    }

    [Fact]
    public void TrailingSeparator_MeansFolder_WithoutLookingAtTheDisk() =>
        Assert.True(FileSystemProbe.IsDirectory(Path.Combine(root.FullName, "missing") + Path.DirectorySeparatorChar));
}

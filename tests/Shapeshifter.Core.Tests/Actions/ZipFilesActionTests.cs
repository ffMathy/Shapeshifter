using System.IO.Compression;
using System.Text;
using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Tests.Actions;

public sealed class ZipFilesActionTests : IDisposable
{
    private readonly string workspace = Directory.CreateTempSubdirectory("shapeshifter-zip-").FullName;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        try
        {
            Directory.Delete(workspace, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    public static readonly TheoryData<string, bool> CanExecuteMatrix = new()
    {
        { "plain", false },
        { "text+html", false },
        { "html-only", false },
        { "image", false },
        { "file", true },
        { "files", true },
        { "file+html", true },
        { "other", false },
        { "empty", false },
        { "image-link", false },
    };

    [Theory]
    [MemberData(nameof(CanExecuteMatrix))]
    public void CanExecute_OnlyWhenFilesAreCopied(string kind, bool expected) =>
        Assert.Equal(expected, new ZipFilesAction().CanExecute(Items.ByName(kind)));

    [Fact]
    public void CreateArchive_ContainsFilesAndNestedFoldersWithExactContent()
    {
        var file = WriteFile("notes.txt", "top level");
        var folder = Path.Combine(workspace, "Project");
        WriteFile(Path.Combine("Project", "readme.md"), "# readme");
        WriteFile(Path.Combine("Project", "src", "deep", "code.cs"), "class C {}");
        var binary = Path.Combine(workspace, "data.bin");
        byte[] binaryContent = [0, 1, 2, 255, 254, 0];
        File.WriteAllBytes(binary, binaryContent);

        var archivePath = ZipFilesAction.CreateArchive([file, folder, binary], Path.Combine(workspace, "temp"));

        using var archive = ZipFile.OpenRead(archivePath);
        Assert.Equal(
            ["Project/readme.md", "Project/src/deep/code.cs", "data.bin", "notes.txt"],
            archive.Entries.Select(entry => entry.FullName).Order(StringComparer.Ordinal));
        Assert.Equal("top level", ReadEntry(archive, "notes.txt"));
        Assert.Equal("# readme", ReadEntry(archive, "Project/readme.md"));
        Assert.Equal("class C {}", ReadEntry(archive, "Project/src/deep/code.cs"));
        Assert.Equal(binaryContent, ReadEntryBytes(archive, "data.bin"));
        Assert.All(archive.Entries, entry => Assert.DoesNotContain('\\', entry.FullName));
    }

    [Fact]
    public void CreateArchive_PlacesArchiveInAFreshSubfolderOfTheTemporaryRoot()
    {
        var file = WriteFile("a.txt", "a");
        var temporaryRoot = Path.Combine(workspace, "temp");

        var first = ZipFilesAction.CreateArchive([file], temporaryRoot);
        var second = ZipFilesAction.CreateArchive([file], temporaryRoot);

        Assert.NotEqual(first, second);
        foreach (var path in new[] { first, second })
        {
            Assert.True(File.Exists(path));
            Assert.Equal("a.zip", Path.GetFileName(path));
            var container = Path.GetDirectoryName(path)!;
            Assert.Equal(Path.Combine(temporaryRoot, "Compressed folders"), Path.GetDirectoryName(container));
            Assert.Equal(8, Path.GetFileName(container).Length);
        }
    }

    [Fact]
    public void CreateArchive_DuplicateFileNames_AreMadeUnique()
    {
        var a = WriteFile(Path.Combine("one", "report.txt"), "one");
        var b = WriteFile(Path.Combine("two", "report.txt"), "two");
        var c = WriteFile(Path.Combine("three", "REPORT.txt"), "three");

        var archivePath = ZipFilesAction.CreateArchive([a, b, c], Path.Combine(workspace, "temp"));

        using var archive = ZipFile.OpenRead(archivePath);
        Assert.Equal(["report.txt", "report (2).txt", "REPORT (3).txt"], archive.Entries.Select(entry => entry.FullName));
        Assert.Equal("one", ReadEntry(archive, "report.txt"));
        Assert.Equal("two", ReadEntry(archive, "report (2).txt"));
        Assert.Equal("three", ReadEntry(archive, "REPORT (3).txt"));
    }

    [Fact]
    public void CreateArchive_DuplicateFolderNames_AreMadeUnique()
    {
        WriteFile(Path.Combine("one", "assets", "x.txt"), "one");
        WriteFile(Path.Combine("two", "assets", "x.txt"), "two");

        var archivePath = ZipFilesAction.CreateArchive(
            [Path.Combine(workspace, "one", "assets"), Path.Combine(workspace, "two", "assets") + Path.DirectorySeparatorChar],
            Path.Combine(workspace, "temp"));

        using var archive = ZipFile.OpenRead(archivePath);
        Assert.Equal(["assets/x.txt", "assets (2)/x.txt"], archive.Entries.Select(entry => entry.FullName));
        Assert.Equal("one", ReadEntry(archive, "assets/x.txt"));
        Assert.Equal("two", ReadEntry(archive, "assets (2)/x.txt"));
    }

    [Fact]
    public void CreateArchive_FileAndFolderWithTheSameName_DoNotCollide()
    {
        var file = WriteFile(Path.Combine("one", "data"), "file");
        WriteFile(Path.Combine("two", "data", "inner.txt"), "inner");

        var archivePath = ZipFilesAction.CreateArchive([file, Path.Combine(workspace, "two", "data")], Path.Combine(workspace, "temp"));

        using var archive = ZipFile.OpenRead(archivePath);
        Assert.Equal(["data", "data (2)/inner.txt"], archive.Entries.Select(entry => entry.FullName));
    }

    [Fact]
    public void CreateArchive_SkipsPathsThatNoLongerExist()
    {
        var file = WriteFile("kept.txt", "kept");

        var archivePath = ZipFilesAction.CreateArchive([Path.Combine(workspace, "gone.txt"), file], Path.Combine(workspace, "temp"));

        using var archive = ZipFile.OpenRead(archivePath);
        Assert.Equal(["kept.txt"], archive.Entries.Select(entry => entry.FullName));
    }

    [Fact]
    public void CreateArchive_RejectsNullPaths() =>
        Assert.Throws<ArgumentNullException>(() => ZipFilesAction.CreateArchive(null!, workspace));

    [Fact]
    public async Task Execute_CopiesTheArchiveAndMakesItTheNewestHistoryItem()
    {
        using var harness = new ActionTestHarness();
        var a = WriteFile(Path.Combine("Holiday", "a.jpg"), "a");
        var b = WriteFile(Path.Combine("Holiday", "b.jpg"), "b");
        var item = harness.AddToHistory(new ClipboardSnapshot { Files = [a, b] });

        await new ZipFilesAction().ExecuteAsync(item, harness.Context, Ct);

        var written = Assert.Single(harness.Backend.Writes);
        var archivePath = Assert.Single(written.Files);
        Assert.Equal("Holiday.zip", Path.GetFileName(archivePath));
        Assert.StartsWith(Path.Combine(harness.Paths.TempDirectory, "Compressed folders") + Path.DirectorySeparatorChar, archivePath, StringComparison.Ordinal);
        Assert.Null(written.Text);
        Assert.Equal(0, harness.Paste.Calls);

        var top = harness.History.Items[0];
        Assert.Equal(ClipboardContentKind.File, top.Kind);
        Assert.Equal([archivePath], top.Snapshot.Files);
        Assert.Equal(item.Id, harness.History.Items[1].Id);

        using var archive = ZipFile.OpenRead(archivePath);
        Assert.Equal(["a.jpg", "b.jpg"], archive.Entries.Select(entry => entry.FullName));
    }

    public static readonly TheoryData<string[], string> ArchiveNames = new()
    {
        { [P("home", "me", "report.txt")], "report" },
        { [P("home", "me", "archive.tar.gz")], "archive.tar" },
        { [P("home", "me", "Photos")], "Photos" },
        { [P("home", "me", "Photos") + Path.DirectorySeparatorChar], "Photos" },
        { [P("home", "me", ".bashrc")], "Archive" },
        { [P("home", "me", "a.txt"), P("home", "me", "b.txt")], "me" },
        { [P("home", "me", "a.txt"), P("home", "me", "Sub") + Path.DirectorySeparatorChar], "me" },
        { [P("home", "me", "a.txt"), P("home", "you", "b.txt")], "Archive" },
        { [P("a.txt"), P("b.txt")], "Archive" },
        { [], "Archive" },
    };

    [Theory]
    [MemberData(nameof(ArchiveNames))]
    public void GetArchiveName_FollowsTheNamingRules(string[] paths, string expected) =>
        Assert.Equal(expected, ZipFilesAction.GetArchiveName(paths));

    [Fact]
    public void GetArchiveName_RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => ZipFilesAction.GetArchiveName(null!));

    private static string P(params string[] parts) =>
        Path.Combine([Path.GetPathRoot(Path.GetTempPath())!, .. parts]);

    private string WriteFile(string relativePath, string content)
    {
        var path = Path.Combine(workspace, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private static string ReadEntry(ZipArchive archive, string name) => Encoding.UTF8.GetString(ReadEntryBytes(archive, name));

    private static byte[] ReadEntryBytes(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name) ?? throw new InvalidOperationException($"Missing entry {name}");
        using var stream = entry.Open();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}

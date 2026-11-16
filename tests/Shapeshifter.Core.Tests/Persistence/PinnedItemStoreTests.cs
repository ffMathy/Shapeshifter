using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Persistence;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Tests.Persistence;

public sealed class PinnedItemStoreTests : IDisposable
{
    private readonly string root;
    private readonly PinnedItemStore store;

    public PinnedItemStoreTests()
    {
        root = Path.Combine(Directory.CreateTempSubdirectory("shapeshifter-pinned-").FullName, "Pinned");
        store = new PinnedItemStore(root);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path.GetDirectoryName(root)!, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public async Task LoadAll_MissingRoot_ReturnsEmpty()
    {
        Assert.False(Directory.Exists(root));

        Assert.Empty(await store.LoadAllAsync(Ct));
    }

    [Fact]
    public async Task Save_CreatesOneFolderPerItemNamedAfterItsId()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Text = "x" });

        await store.SaveAsync(item, Ct);

        var folder = Assert.Single(Directory.GetDirectories(root));
        Assert.Equal(item.Id.ToString("N"), Path.GetFileName(folder));
        Assert.True(File.Exists(Path.Combine(folder, "manifest.json")));
        Assert.Empty(Directory.GetDirectories(root, "*.saving"));
    }

    [Fact]
    public async Task RoundTrip_Source_IsKept()
    {
        var source = new ClipboardSource("Google Chrome", "Shapeshifter - Wikipedia – Google Chrome", [0x89, 0x50, 0x4E, 0x47]);
        var item = new ClipboardItem(new ClipboardSnapshot { Text = "x" }, source: source);

        await store.SaveAsync(item, Ct);
        var loaded = Assert.Single(await store.LoadAllAsync(Ct));

        Assert.Equal(source.ApplicationName, loaded.Source?.ApplicationName);
        Assert.Equal(source.WindowTitle, loaded.Source?.WindowTitle);
        Assert.Equal(source.IconPng, loaded.Source?.IconPng);
    }

    [Fact]
    public async Task RoundTrip_WithoutSource_HasNoSource()
    {
        await store.SaveAsync(new ClipboardItem(new ClipboardSnapshot { Text = "x" }), Ct);

        Assert.Null(Assert.Single(await store.LoadAllAsync(Ct)).Source);
    }

    [Fact]
    public async Task RoundTrip_Text_IsExact()
    {
        const string text = "Grüße 世界 👋\r\nsecond line\ttab\n\u00A0trailing  ";
        var capturedAt = new DateTimeOffset(2023, 12, 31, 23, 59, 58, 123, TimeSpan.FromHours(5.5)).AddTicks(4567);
        var item = new ClipboardItem(new ClipboardSnapshot { Text = text }, Guid.NewGuid(), capturedAt);

        await store.SaveAsync(item, Ct);
        var loaded = Assert.Single(await store.LoadAllAsync(Ct));

        Assert.Equal(item.Id, loaded.Id);
        Assert.Equal(capturedAt, loaded.CapturedAt);
        Assert.Equal(capturedAt.Offset, loaded.CapturedAt.Offset);
        Assert.Equal(capturedAt.Ticks, loaded.CapturedAt.Ticks);
        Assert.Equal(text, loaded.Snapshot.Text);
        Assert.Null(loaded.Snapshot.Image);
        Assert.Empty(loaded.Snapshot.Files);
        Assert.Empty(loaded.Snapshot.RawFormats);
        Assert.Equal(item.Snapshot.Fingerprint, loaded.Snapshot.Fingerprint);
        Assert.Equal(item.PreviewText, loaded.PreviewText);
    }

    [Fact]
    public async Task RoundTrip_EmptyText_StaysEmptyNotNull()
    {
        var item = new ClipboardItem(new ClipboardSnapshot
        {
            Text = string.Empty,
            RawFormats = [new RawClipboardFormat("text/html", Encoding.UTF8.GetBytes("<p>x</p>"))],
        });

        await store.SaveAsync(item, Ct);
        var loaded = Assert.Single(await store.LoadAllAsync(Ct));

        Assert.Equal(string.Empty, loaded.Snapshot.Text);
        Assert.Equal(ClipboardContentKind.Text, loaded.Kind);
    }

    [Fact]
    public async Task RoundTrip_TextStartingWithByteOrderMark_IsPreserved()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Text = "\uFEFFbom first" });

        await store.SaveAsync(item, Ct);
        var loaded = Assert.Single(await store.LoadAllAsync(Ct));

        Assert.Equal("\uFEFFbom first", loaded.Snapshot.Text);
    }

    [Fact]
    public async Task RoundTrip_Files_PreservesPathsAndOrder()
    {
        string[] files = ["/home/me/b.txt", "/home/me/a folder/", "C:\\Users\\me\\ü.doc", "/home/me/a.txt"];
        var item = new ClipboardItem(new ClipboardSnapshot { Files = files });

        await store.SaveAsync(item, Ct);
        var loaded = Assert.Single(await store.LoadAllAsync(Ct));

        Assert.Equal(files, loaded.Snapshot.Files);
        Assert.Null(loaded.Snapshot.Text);
        Assert.Equal(ClipboardContentKind.FileCollection, loaded.Kind);
    }

    [Fact]
    public async Task RoundTrip_Image_IsByteExact()
    {
        var image = new byte[70_000];
        new Random(42).NextBytes(image);
        var item = new ClipboardItem(new ClipboardSnapshot { Image = image });

        await store.SaveAsync(item, Ct);
        var loaded = Assert.Single(await store.LoadAllAsync(Ct));

        Assert.Equal(image, loaded.Snapshot.Image);
        Assert.Equal(ClipboardContentKind.Image, loaded.Kind);
    }

    [Fact]
    public async Task RoundTrip_EmptyImage_IsKept()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Image = [] });

        await store.SaveAsync(item, Ct);
        var loaded = Assert.Single(await store.LoadAllAsync(Ct));

        Assert.NotNull(loaded.Snapshot.Image);
        Assert.Empty(loaded.Snapshot.Image);
    }

    [Fact]
    public async Task RoundTrip_RawFormats_KeepNamesBytesAndOrder()
    {
        RawClipboardFormat[] formats =
        [
            new("text/html", Encoding.UTF8.GetBytes("<b>hi</b>")),
            new("application/x-vnd.custom;foo=\"bar\"", [0, 255, 0, 128]),
            new("Empty", []),
            new("HTML Format", Encoding.Unicode.GetBytes("utf16")),
            new("text/html", Encoding.UTF8.GetBytes("duplicate name")),
        ];
        var item = new ClipboardItem(new ClipboardSnapshot { Text = "hi", Image = [9], Files = ["/x"], RawFormats = formats });

        await store.SaveAsync(item, Ct);
        var loaded = Assert.Single(await store.LoadAllAsync(Ct));

        Assert.Equal(formats.Length, loaded.Snapshot.RawFormats.Count);
        for (var i = 0; i < formats.Length; i++)
        {
            Assert.Equal(formats[i].Name, loaded.Snapshot.RawFormats[i].Name);
            Assert.Equal(formats[i].Data, loaded.Snapshot.RawFormats[i].Data);
        }

        Assert.Equal("hi", loaded.Snapshot.Text);
        Assert.Equal(new byte[] { 9 }, loaded.Snapshot.Image);
        Assert.Equal(["/x"], loaded.Snapshot.Files);
    }

    [Fact]
    public async Task RoundTrip_EmptySnapshot()
    {
        var item = new ClipboardItem(ClipboardSnapshot.Empty);

        await store.SaveAsync(item, Ct);
        var loaded = Assert.Single(await store.LoadAllAsync(Ct));

        Assert.True(loaded.Snapshot.IsEmpty);
        Assert.Equal(item.Id, loaded.Id);
    }

    [Fact]
    public async Task Save_SameItemTwice_OverwritesWithoutLeftovers()
    {
        var id = Guid.NewGuid();
        var at = DateTimeOffset.UtcNow;
        await store.SaveAsync(new ClipboardItem(new ClipboardSnapshot { Text = "old", Image = [1] }, id, at), Ct);

        await store.SaveAsync(new ClipboardItem(new ClipboardSnapshot { Text = "new" }, id, at), Ct);

        var folder = Assert.Single(Directory.GetDirectories(root));
        Assert.False(File.Exists(Path.Combine(folder, "image.bin")));
        var loaded = Assert.Single(await store.LoadAllAsync(Ct));
        Assert.Equal("new", loaded.Snapshot.Text);
        Assert.Null(loaded.Snapshot.Image);
    }

    [Fact]
    public async Task Save_LeftoverStagingFolder_IsReplaced()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Text = "x" });
        var staging = Path.Combine(root, item.Id.ToString("N") + ".saving");
        Directory.CreateDirectory(staging);
        await File.WriteAllTextAsync(Path.Combine(staging, "junk"), "junk", Ct);

        await store.SaveAsync(item, Ct);

        Assert.False(Directory.Exists(staging));
        var folder = Assert.Single(Directory.GetDirectories(root));
        Assert.False(File.Exists(Path.Combine(folder, "junk")));
    }

    [Fact]
    public async Task Save_RejectsNull() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveAsync(null!, Ct));

    [Fact]
    public async Task LoadAll_ReturnsOldestPinFirst()
    {
        var items = Enumerable.Range(0, 4)
            .Select(i => new ClipboardItem(new ClipboardSnapshot { Text = $"item {i}" }))
            .ToArray();
        foreach (var item in items)
        {
            await store.SaveAsync(item, Ct);
        }

        // Pin times are taken from the folder timestamps; make them unambiguous and not alphabetical.
        var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        int[] pinOrder = [2, 0, 3, 1];
        for (var rank = 0; rank < pinOrder.Length; rank++)
        {
            Directory.SetLastWriteTimeUtc(Folder(items[pinOrder[rank]].Id), baseTime.AddMinutes(rank));
        }

        var loaded = await store.LoadAllAsync(Ct);

        Assert.Equal(pinOrder.Select(i => items[i].Id), loaded.Select(item => item.Id));
    }

    [Fact]
    public async Task LoadAll_SequentialSaves_KeepSaveOrder()
    {
        var first = new ClipboardItem(new ClipboardSnapshot { Text = "first" });
        var second = new ClipboardItem(new ClipboardSnapshot { Text = "second" });
        await store.SaveAsync(first, Ct);
        await Task.Delay(30, Ct);
        await store.SaveAsync(second, Ct);

        var loaded = await store.LoadAllAsync(Ct);

        Assert.Equal([first.Id, second.Id], loaded.Select(item => item.Id));
    }

    [Fact]
    public async Task LoadAll_ReturnsUnpinnedItems()
    {
        await store.SaveAsync(new ClipboardItem(new ClipboardSnapshot { Text = "x" }), Ct);

        var loaded = Assert.Single(await store.LoadAllAsync(Ct));

        Assert.False(loaded.IsPinned);
    }

    [Fact]
    public async Task Delete_RemovesOnlyThatItem()
    {
        var keep = new ClipboardItem(new ClipboardSnapshot { Text = "keep" });
        var drop = new ClipboardItem(new ClipboardSnapshot { Text = "drop" });
        await store.SaveAsync(keep, Ct);
        await store.SaveAsync(drop, Ct);

        await store.DeleteAsync(drop.Id);

        Assert.False(Directory.Exists(Folder(drop.Id)));
        Assert.Equal([keep.Id], (await store.LoadAllAsync(Ct)).Select(item => item.Id));
    }

    [Fact]
    public async Task Delete_UnknownId_IsANoOp()
    {
        await store.DeleteAsync(Guid.NewGuid());
        Directory.CreateDirectory(root);
        await store.DeleteAsync(Guid.NewGuid());

        Assert.Empty(await store.LoadAllAsync(Ct));
    }

    [Fact]
    public async Task LoadAll_SkipsStagingFolders()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Text = "real" });
        await store.SaveAsync(item, Ct);
        var orphan = new ClipboardItem(new ClipboardSnapshot { Text = "half saved" });
        await store.SaveAsync(orphan, Ct);
        Directory.Move(Folder(orphan.Id), Folder(orphan.Id) + ".saving");

        var loaded = await store.LoadAllAsync(Ct);

        Assert.Equal([item.Id], loaded.Select(i => i.Id));
    }

    [Fact]
    public async Task LoadAll_SkipsFoldersWithoutManifestAndStrayFiles()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Text = "real" });
        await store.SaveAsync(item, Ct);
        Directory.CreateDirectory(Path.Combine(root, "empty-folder"));
        await File.WriteAllTextAsync(Path.Combine(root, "stray.txt"), "x", Ct);

        var loaded = await store.LoadAllAsync(Ct);

        Assert.Equal([item.Id], loaded.Select(i => i.Id));
    }

    [Theory]
    [InlineData("{ this is not json")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("[1, 2, 3]")]
    [InlineData("{\"Version\": \"one\"}")]
    [InlineData("{\"Version\": 1, \"Id\": \"not-a-guid\"}")]
    public async Task LoadAll_SkipsCorruptManifests(string manifest)
    {
        var good = new ClipboardItem(new ClipboardSnapshot { Text = "good" });
        await store.SaveAsync(good, Ct);
        var bad = Path.Combine(root, "bad");
        Directory.CreateDirectory(bad);
        await File.WriteAllTextAsync(Path.Combine(bad, "manifest.json"), manifest, Ct);

        var loaded = await store.LoadAllAsync(Ct);

        Assert.Equal([good.Id], loaded.Select(i => i.Id));
    }

    [Fact]
    public async Task LoadAll_SkipsManifestsFromANewerVersion()
    {
        var good = new ClipboardItem(new ClipboardSnapshot { Text = "good" });
        var future = new ClipboardItem(new ClipboardSnapshot { Text = "future" });
        await store.SaveAsync(good, Ct);
        await store.SaveAsync(future, Ct);
        await EditManifestAsync(future.Id, manifest => manifest["Version"] = 2);

        var loaded = await store.LoadAllAsync(Ct);

        Assert.Equal([good.Id], loaded.Select(i => i.Id));
    }

    [Fact]
    public async Task LoadAll_SkipsItemsWhosePartFileIsMissing()
    {
        var good = new ClipboardItem(new ClipboardSnapshot { Text = "good" });
        var broken = new ClipboardItem(new ClipboardSnapshot { Image = [1, 2, 3] });
        await store.SaveAsync(good, Ct);
        await store.SaveAsync(broken, Ct);
        File.Delete(Path.Combine(Folder(broken.Id), "image.bin"));

        var loaded = await store.LoadAllAsync(Ct);

        Assert.Equal([good.Id], loaded.Select(i => i.Id));
    }

    public static readonly TheoryData<string, string> TraversalReferences = new()
    {
        { "TextFile", "../secret.txt" },
        { "TextFile", "../../outside/secret.txt" },
        { "ImageFile", "sub/../../secret.txt" },
        { "TextFile", "" },
        { "TextFile", "." },
        { "ImageFile", "SECRET_ABSOLUTE" },
        { "RawFormats", "../secret.txt" },
        { "RawFormats", "SECRET_ABSOLUTE" },
    };

    [Theory]
    [MemberData(nameof(TraversalReferences))]
    public async Task LoadAll_RejectsFileReferencesOutsideTheItemFolder(string property, string reference)
    {
        var secret = Path.Combine(root, "secret.txt");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(secret, "do not read", Ct);
        reference = reference == "SECRET_ABSOLUTE" ? secret : reference.Replace('/', Path.DirectorySeparatorChar);

        var good = new ClipboardItem(new ClipboardSnapshot { Text = "good" });
        var evil = new ClipboardItem(new ClipboardSnapshot
        {
            Text = "evil",
            Image = [1],
            RawFormats = [new RawClipboardFormat("text/html", [1])],
        });
        await store.SaveAsync(good, Ct);
        await store.SaveAsync(evil, Ct);
        await EditManifestAsync(evil.Id, manifest =>
        {
            if (property == "RawFormats")
            {
                manifest["RawFormats"]![0]!["File"] = reference;
            }
            else
            {
                manifest[property] = reference;
            }
        });

        var loaded = await store.LoadAllAsync(Ct);

        Assert.Equal([good.Id], loaded.Select(i => i.Id));
    }

    [Fact]
    public async Task LoadAll_AcceptsReferencesInsideSubfoldersOfTheItemFolder()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Text = "nested" });
        await store.SaveAsync(item, Ct);
        var folder = Folder(item.Id);
        Directory.CreateDirectory(Path.Combine(folder, "sub"));
        File.Move(Path.Combine(folder, "text.txt"), Path.Combine(folder, "sub", "text.txt"));
        await EditManifestAsync(item.Id, manifest => manifest["TextFile"] = "sub/text.txt");

        var loaded = Assert.Single(await store.LoadAllAsync(Ct));

        Assert.Equal("nested", loaded.Snapshot.Text);
    }

    [Fact]
    public async Task Manifest_UsesFormatNamesAndRelativeFileNames()
    {
        var item = new ClipboardItem(new ClipboardSnapshot
        {
            Text = "t",
            Image = [1],
            RawFormats = [new RawClipboardFormat("public.html", [1]), new RawClipboardFormat("text/rtf", [2])],
        });

        await store.SaveAsync(item, Ct);

        var manifest = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(Folder(item.Id), "manifest.json"), Ct))!;
        Assert.Equal(1, manifest["Version"]!.GetValue<int>());
        Assert.Equal(item.Id, manifest["Id"]!.GetValue<Guid>());
        Assert.Equal("text.txt", manifest["TextFile"]!.GetValue<string>());
        Assert.Equal("image.bin", manifest["ImageFile"]!.GetValue<string>());
        var raw = manifest["RawFormats"]!.AsArray();
        Assert.Equal(["public.html", "text/rtf"], raw.Select(node => node!["Name"]!.GetValue<string>()));
        Assert.Equal(["raw-000.bin", "raw-001.bin"], raw.Select(node => node!["File"]!.GetValue<string>()));
    }

    /// <summary>
    /// <see cref="PinnedItemStore.LoadAllAsync"/> promises that unreadable folders are skipped. A hand-edited or
    /// truncated manifest with explicit nulls must not make loading every other pinned item fail.
    /// </summary>
    [Theory]
    [InlineData("Files")]
    [InlineData("RawFormats")]
    [InlineData("RawFormatFile")]
    public async Task LoadAll_SkipsManifestsWithNullCollectionsOrNames(string property)
    {
        var good = new ClipboardItem(new ClipboardSnapshot { Text = "good" });
        var bad = new ClipboardItem(new ClipboardSnapshot { Text = "bad", RawFormats = [new RawClipboardFormat("text/html", [1])] });
        await store.SaveAsync(good, Ct);
        await store.SaveAsync(bad, Ct);
        await EditManifestAsync(bad.Id, manifest =>
        {
            if (property == "RawFormatFile")
            {
                manifest["RawFormats"]![0]!["File"] = null;
            }
            else
            {
                manifest[property] = null;
            }
        });

        var loaded = await store.LoadAllAsync(Ct);

        Assert.Contains(good.Id, loaded.Select(i => i.Id));
    }

    private string Folder(Guid id) => Path.Combine(root, id.ToString("N"));

    private async Task EditManifestAsync(Guid id, Action<JsonObject> edit)
    {
        var path = Path.Combine(Folder(id), "manifest.json");
        var manifest = JsonNode.Parse(await File.ReadAllTextAsync(path, Ct))!.AsObject();
        edit(manifest);
        await File.WriteAllTextAsync(path, manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), Ct);
    }
}

using System.Text;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Tests.Clipboard;

public sealed class HistorySearchTests
{
    private static bool Finds(ClipboardItem item, string query) =>
        HistorySearch.Matches(HistorySearch.CreateSearchText(HistorySearch.CreateContentText(item), item), HistorySearch.ParseQuery(query));

    private static ClipboardItem Text(string text, ClipboardSource? source = null) => new(new ClipboardSnapshot { Text = text }, source: source);

    [Theory]
    [InlineData("hello")]
    [InlineData("HELLO")]
    [InlineData("lo wo")]
    [InlineData("world hello")]
    [InlineData("  hello   world  ")]
    [InlineData("")]
    [InlineData("   ")]
    public void Text_IsFound_IgnoringCaseAndWordOrder(string query) => Assert.True(Finds(Text("Hello World"), query));

    [Theory]
    [InlineData("goodbye")]
    [InlineData("hello there")]
    [InlineData("helloworld")]
    public void Text_IsNotFound_UnlessEveryWordOccurs(string query) => Assert.False(Finds(Text("Hello World"), query));

    [Theory]
    [InlineData("cafe", "Café au lait")]
    [InlineData("CAFÉ", "cafe au lait")]
    [InlineData("naive", "naïve")]
    [InlineData("übung", "Ubung")]
    public void Accents_AreIgnored(string query, string text) => Assert.True(Finds(Text(text), query));

    [Fact]
    public void TheWholeText_IsSearched_NotOnlyThePreview()
    {
        var item = Text("first line\n" + string.Concat(Enumerable.Repeat("filler ", 200)) + "\nneedle at the end");

        Assert.DoesNotContain("needle", item.PreviewText, StringComparison.Ordinal);
        Assert.True(Finds(item, "needle"));
    }

    [Fact]
    public void VeryLongText_IsOnlySearchedUpToTheLimit()
    {
        var item = Text(new string('a', HistorySearch.MaxContentLength) + " needle");

        Assert.False(Finds(item, "needle"));
    }

    [Fact]
    public void HtmlOnlyItems_AreSearchedByTheirPlainText()
    {
        var item = new ClipboardItem(new ClipboardSnapshot
        {
            RawFormats = [new RawClipboardFormat("text/html", Encoding.UTF8.GetBytes("<p><b>Quarterly</b> report</p>"))],
        });

        Assert.True(Finds(item, "quarterly report"));
        Assert.False(Finds(item, "<b>"));
    }

    [Fact]
    public void Files_AreSearchedByTheirPaths()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Files = ["/home/me/Projects/budget-2026.xlsx", "/home/me/Notes/todo.txt"] });

        Assert.True(Finds(item, "budget"));
        Assert.True(Finds(item, "todo.txt"));
        Assert.True(Finds(item, "projects"));
        Assert.True(Finds(item, "files"));
    }

    [Fact]
    public void Images_AreFoundByTheWordImage()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Image = [1, 2, 3] });

        Assert.True(Finds(item, "image"));
        Assert.False(Finds(Text("plain"), "image"));
    }

    [Fact]
    public void TheApplicationAndWindowItWasCopiedFrom_AreSearched()
    {
        var item = Text("some text", new ClipboardSource("Google Chrome", "Pull request – GitHub", null));

        Assert.True(Finds(item, "chrome"));
        Assert.True(Finds(item, "github text"));
        Assert.False(Finds(Text("some text"), "chrome"));
    }

    [Fact]
    public void ParseQuery_SplitsNormalizesAndDeduplicates()
    {
        Assert.Equal(["cafe", "menu"], HistorySearch.ParseQuery(" Café\tMENU cafe "));
        Assert.Empty(HistorySearch.ParseQuery(null));
        Assert.Empty(HistorySearch.ParseQuery(" \n "));
    }
}

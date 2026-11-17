using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Previews;

namespace Shapeshifter.Core.Tests.Previews;

public sealed class LinkedImageTests
{
    [Theory]
    [InlineData("https://example.com/cat.png", "https://example.com/cat.png")]
    [InlineData("  https://i.example.com/a/b.JPEG?size=large  ", "https://i.example.com/a/b.JPEG?size=large")]
    [InlineData("www.example.com/cat.webp", "https://www.example.com/cat.webp")]
    [InlineData("(https://example.com/cat.gif)", "https://example.com/cat.gif")]
    [InlineData("https://example.com/cat.png\n", "https://example.com/cat.png")]
    public void Find_AnImageLinkOnItsOwn_IsALinkedImage(string text, string expected)
    {
        var image = LinkedImage.Find(text);

        Assert.NotNull(image);
        Assert.Equal(new Uri(expected), image.Link);
        Assert.Null(image.FilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://example.com/page")]
    [InlineData("https://example.com/cat.svg")]
    [InlineData("look at https://example.com/cat.png")]
    [InlineData("https://example.com/cat.png https://example.com/dog.png")]
    [InlineData("https://example.com/cat.png\nhttps://example.com/dog.png")]
    [InlineData("ftp://example.com/cat.png")]
    [InlineData("cat.png")]
    [InlineData("Pictures/cat.png")]
    [InlineData("/home/me/notes.txt")]
    [InlineData("#2c5497")]
    public void Find_AnythingElse_IsNotALinkedImage(string? text) => Assert.Null(LinkedImage.Find(text));

    [Theory]
    [InlineData("/home/me/Pictures/cat.png", "/home/me/Pictures/cat.png")]
    [InlineData("/home/me/My Pictures/cat.jpg", "/home/me/My Pictures/cat.jpg")]
    [InlineData(@"C:\Users\me\Pictures\cat.png", @"C:\Users\me\Pictures\cat.png")]
    [InlineData(@"""C:\Users\me\My Pictures\cat.png""", @"C:\Users\me\My Pictures\cat.png")]
    [InlineData(@"\\server\share\cat.bmp", @"\\server\share\cat.bmp")]
    public void Find_ThePathOfAnImageFile_IsALinkedImage(string text, string expected)
    {
        var image = LinkedImage.Find(text);

        Assert.NotNull(image);
        Assert.Null(image.Link);
        Assert.Equal(expected, image.FilePath);
    }

    [Fact]
    public void Find_AFileAddress_IsTheImageFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "some folder", "cat.png");

        var image = LinkedImage.Find(new Uri(path).AbsoluteUri);

        Assert.NotNull(image);
        Assert.Equal(path, image.FilePath);
    }

    [Fact]
    public void Find_OnlyTextItems_CanBeLinkedImages()
    {
        Assert.NotNull(LinkedImage.Find(new ClipboardItem(new ClipboardSnapshot { Text = "https://example.com/cat.png" })));
        Assert.Null(LinkedImage.Find(new ClipboardItem(new ClipboardSnapshot { Text = "https://example.com/cat.png", Files = ["/tmp/cat.png"] })));
    }

    [Fact]
    public void Location_IsTheSiteOfALink_OrTheFolderOfAFile()
    {
        Assert.Equal("i.example.com", LinkedImage.Find("https://i.example.com/a/cat.png")!.Location);
        Assert.Equal(Path.GetDirectoryName("/home/me/Pictures/cat.png"), LinkedImage.Find("/home/me/Pictures/cat.png")!.Location);
    }
}

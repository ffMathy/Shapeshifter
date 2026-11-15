using Shapeshifter.Core.Text;

namespace Shapeshifter.Core.Tests.Text;

public sealed class TextPreviewTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t ")]
    public void Create_NullOrWhitespace_ReturnsEmpty(string? text)
    {
        Assert.Equal(string.Empty, TextPreview.Create(text));
    }

    [Fact]
    public void Create_SimpleText_IsUnchanged()
    {
        Assert.Equal("hello world", TextPreview.Create("hello world"));
    }

    [Fact]
    public void Create_TrimsLeadingAndTrailingWhitespace()
    {
        Assert.Equal("hello", TextPreview.Create("  \n\thello \r\n "));
    }

    [Fact]
    public void Create_CollapsesWhitespaceRunsIntoSingleSpace()
    {
        Assert.Equal("a b c d", TextPreview.Create("a   b\r\n\r\nc\t\t d"));
    }

    [Fact]
    public void Create_ProducesSingleLine()
    {
        var preview = TextPreview.Create("line one\nline two\r\nline three\u2028line four");

        Assert.Equal("line one line two line three line four", preview);
        Assert.DoesNotContain('\n', preview);
        Assert.DoesNotContain('\r', preview);
    }

    [Fact]
    public void Create_TreatsControlCharactersAsSpaces()
    {
        Assert.Equal("a b c", TextPreview.Create("a\0b\u0007\u001Bc"));
    }

    [Fact]
    public void Create_TreatsNonBreakingSpaceAsWhitespace()
    {
        Assert.Equal("a b", TextPreview.Create("a\u00A0\u00A0b"));
    }

    [Fact]
    public void Create_TrailingControlCharacters_AreRemoved()
    {
        Assert.Equal("text", TextPreview.Create("text\0\0\0"));
    }

    [Fact]
    public void Create_LeadingControlCharacters_AreRemoved()
    {
        // The preview is documented as trimmed; a leading NUL/control character (as left by some native
        // clipboard producers) must not turn into a leading space.
        Assert.Equal("text", TextPreview.Create("\0text"));
    }

    [Fact]
    public void Create_DefaultMaxLength_Is512()
    {
        Assert.Equal(512, TextPreview.MaxLength);
        Assert.Equal(new string('x', 512), TextPreview.Create(new string('x', 2000)));
    }

    [Fact]
    public void Create_TruncatesToCustomMaxLength()
    {
        Assert.Equal("abcde", TextPreview.Create("abcdefghij", 5));
    }

    [Fact]
    public void Create_TextExactlyMaxLength_IsKept()
    {
        Assert.Equal("abcde", TextPreview.Create("abcde", 5));
    }

    [Fact]
    public void Create_TruncationAtSpace_DoesNotEndWithSpace()
    {
        Assert.Equal("abc", TextPreview.Create("abc   def", 4));
    }

    [Fact]
    public void Create_MaxLengthCountsCollapsedOutput()
    {
        // 6 output characters "a b c " fit even though the input is much longer.
        Assert.Equal("a b c", TextPreview.Create("a      b      c      d", 6));
    }

    [Fact]
    public void Create_ZeroMaxLength_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, TextPreview.Create("abc", 0));
    }

    [Fact]
    public void Create_NeverExceedsMaxLength()
    {
        var random = new Random(1234);
        // Leading control characters are covered separately (Create_LeadingControlCharacters_AreRemoved).
        const string alphabet = "ab \t\n\r";
        for (var i = 0; i < 200; i++)
        {
            var chars = new char[random.Next(0, 80)];
            for (var j = 0; j < chars.Length; j++)
            {
                chars[j] = alphabet[random.Next(alphabet.Length)];
            }

            var max = random.Next(1, 40);
            var preview = TextPreview.Create(new string(chars), max);

            Assert.True(preview.Length <= max, $"'{preview}' is longer than {max}");
            Assert.Equal(preview.Trim(), preview);
            Assert.DoesNotContain("  ", preview);
        }
    }

    [Fact]
    public void Create_PreservesUnicodeLetters()
    {
        Assert.Equal("Grüße 日本語 😀", TextPreview.Create("Grüße\n日本語\t😀"));
    }

    [Fact]
    public void Create_TruncationDoesNotSplitSurrogatePair()
    {
        // "ab😀" is 4 UTF-16 units; truncating to 3 must not leave a lone high surrogate.
        var preview = TextPreview.Create("ab😀cd", 3);

        Assert.False(char.IsHighSurrogate(preview[^1]), "Preview ends with an unpaired high surrogate.");
    }
}

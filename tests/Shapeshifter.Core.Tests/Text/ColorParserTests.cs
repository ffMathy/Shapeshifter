using Shapeshifter.Core.Text;

namespace Shapeshifter.Core.Tests.Text;

public sealed class ColorParserTests
{
    [Theory]
    [InlineData("#2c5497", 0x2C, 0x54, 0x97, 255)]
    [InlineData("#2C5497", 0x2C, 0x54, 0x97, 255)]
    [InlineData("  #2c5497\n", 0x2C, 0x54, 0x97, 255)]
    [InlineData("#2c5497;", 0x2C, 0x54, 0x97, 255)]
    [InlineData("#fff", 255, 255, 255, 255)]
    [InlineData("#f00a", 255, 0, 0, 0xAA)]
    [InlineData("#00000080", 0, 0, 0, 0x80)]
    [InlineData("rgb(44, 84, 151)", 44, 84, 151, 255)]
    [InlineData("RGB(44,84,151)", 44, 84, 151, 255)]
    [InlineData("rgba(44, 84, 151, 0.5)", 44, 84, 151, 128)]
    [InlineData("rgb(44 84 151 / 50%)", 44, 84, 151, 128)]
    [InlineData("rgb(100%, 0%, 50%)", 255, 0, 128, 255)]
    public void TryParse_RecognisesColourCodes(string text, int r, int g, int b, int a)
    {
        Assert.True(ColorParser.TryParse(text, out var color));
        Assert.Equal(new ParsedColor((byte)r, (byte)g, (byte)b, (byte)a), color);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("2c5497")]
    [InlineData("#")]
    [InlineData("#12")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("#123456789")]
    [InlineData("#ggg")]
    [InlineData("#fff is white")]
    [InlineData("color: #fff")]
    [InlineData("#hashtag")]
    [InlineData("rgb(1, 2)")]
    [InlineData("rgb(1, 2, 300)")]
    [InlineData("rgb(-1, 2, 3)")]
    [InlineData("rgba(1, 2, 3, 2)")]
    [InlineData("rgb(1, 2, 3")]
    [InlineData("hsl(1, 2%, 3%)")]
    [InlineData("rgb(a, b, c)")]
    public void TryParse_RejectsEverythingElse(string? text)
    {
        Assert.False(ColorParser.TryParse(text, out _));
    }

    [Fact]
    public void TryParse_IgnoresLongTexts()
    {
        Assert.False(ColorParser.TryParse("#fff" + new string(' ', 100), out _));
    }
}

using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Tests.Clipboard;

public sealed class ImageFormatTests
{
    public static readonly TheoryData<byte[], string, string> Signatures = new()
    {
        { [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0], "png", "image/png" },
        { [0xFF, 0xD8, 0xFF, 0xDB], "jpg", "image/jpeg" },
        { "GIF87a..."u8.ToArray(), "gif", "image/gif" },
        { "GIF89a..."u8.ToArray(), "gif", "image/gif" },
        { "BM0123456789AB"u8.ToArray(), "bmp", "image/bmp" },
        { "RIFF\0\0\0\0WEBPVP8 "u8.ToArray(), "webp", "image/webp" },
        { "II*\0...."u8.ToArray(), "tiff", "image/tiff" },
        { "MM\0*...."u8.ToArray(), "tiff", "image/tiff" },
        { "RIFF\0\0\0\0WAVE"u8.ToArray(), "png", "image/png" },
        { "BM"u8.ToArray(), "png", "image/png" },
        { [], "png", "image/png" },
    };

    [Theory]
    [MemberData(nameof(Signatures))]
    public void RecognisesFormatsByTheirFirstBytes(byte[] data, string extension, string mediaType)
    {
        Assert.Equal(extension, ImageFormat.GetExtension(data));
        Assert.Equal(mediaType, ImageFormat.GetMediaType(data));
    }
}

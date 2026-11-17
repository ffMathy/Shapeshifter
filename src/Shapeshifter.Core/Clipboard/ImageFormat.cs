namespace Shapeshifter.Core.Clipboard;

/// <summary>Recognises encoded images by their first bytes.</summary>
public static class ImageFormat
{
    /// <summary>
    /// The usual file extension (without the dot) for the encoded image in <paramref name="data"/>. Data that is not
    /// recognised is assumed to be PNG, the format images are normally captured in.
    /// </summary>
    public static string GetExtension(ReadOnlySpan<byte> data) => GetMediaType(data) switch
    {
        "image/jpeg" => "jpg",
        "image/gif" => "gif",
        "image/bmp" => "bmp",
        "image/webp" => "webp",
        "image/tiff" => "tiff",
        _ => "png",
    };

    /// <summary>The media type of the encoded image in <paramref name="data"/>; PNG when it is not recognised.</summary>
    public static string GetMediaType(ReadOnlySpan<byte> data)
    {
        if (data.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]))
        {
            return "image/jpeg";
        }

        if (data.StartsWith("GIF87a"u8) || data.StartsWith("GIF89a"u8))
        {
            return "image/gif";
        }

        if (data.StartsWith("BM"u8) && data.Length >= 14)
        {
            return "image/bmp";
        }

        if (data.Length >= 12 && data.StartsWith("RIFF"u8) && data[8..12].SequenceEqual("WEBP"u8))
        {
            return "image/webp";
        }

        if (data.StartsWith("II*\0"u8) || data.StartsWith("MM\0*"u8))
        {
            return "image/tiff";
        }

        return "image/png";
    }
}

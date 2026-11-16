using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Shapeshifter.Core.Clipboard;

/// <summary>
/// A raw, platform-specific clipboard format (for example <c>text/html</c> on Linux,
/// <c>HTML Format</c> on Windows or <c>public.html</c> on macOS) together with its bytes.
/// Raw formats are kept so that pasting an item restores it byte-for-byte, including
/// rich text and application-private formats.
/// </summary>
public sealed record RawClipboardFormat(string Name, byte[] Data);

/// <summary>What an item primarily represents. Determines how it is displayed and which actions apply.</summary>
public enum ClipboardContentKind
{
    Empty,
    Text,
    Image,
    File,
    FileCollection,
    Other,
}

/// <summary>
/// An immutable copy of everything that was on the clipboard at one point in time.
/// </summary>
/// <remarks>
/// The well-known parts (<see cref="Text"/>, <see cref="Files"/>, <see cref="Image"/>) are read through the
/// UI framework's portable clipboard API; everything else is kept in <see cref="RawFormats"/>.
/// </remarks>
public sealed class ClipboardSnapshot
{
    private string? fingerprint;

    public static ClipboardSnapshot Empty { get; } = new();

    /// <summary>Plain text, if any.</summary>
    public string? Text { get; init; }

    /// <summary>Absolute local paths of copied files and folders.</summary>
    public IReadOnlyList<string> Files { get; init; } = [];

    /// <summary>An encoded image (PNG preferred; any format the UI toolkit can decode is accepted when writing).</summary>
    public byte[]? Image { get; init; }

    /// <summary>Additional raw platform formats, in the order the clipboard reported them.</summary>
    public IReadOnlyList<RawClipboardFormat> RawFormats { get; init; } = [];

    public bool IsEmpty =>
        Text is null && Files.Count == 0 && Image is null && RawFormats.Count == 0;

    /// <summary>
    /// The primary kind. Files win over text, and text wins over images: office suites and browsers
    /// commonly put a rendered picture of copied text on the clipboard, which should still read as text.
    /// </summary>
    /// <remarks>
    /// One exception: an image together with a single image file in the temporary directory is an image. That is
    /// how Firefox copies images on Windows; the file is a temporary copy that Firefox deletes later (issue #606).
    /// </remarks>
    public ClipboardContentKind Kind
    {
        get
        {
            if (Image is not null && Files.Count == 1 && IsTemporaryImageFile(Files[0]))
            {
                return ClipboardContentKind.Image;
            }

            if (Files.Count > 1)
            {
                return ClipboardContentKind.FileCollection;
            }

            if (Files.Count == 1)
            {
                return ClipboardContentKind.File;
            }

            if (!string.IsNullOrEmpty(Text))
            {
                return ClipboardContentKind.Text;
            }

            if (Image is not null)
            {
                return ClipboardContentKind.Image;
            }

            if (Text is not null || RawFormats.Count > 0)
            {
                return RawFormats.Any(f => ClipboardFormatClassifier.IsRichText(f.Name))
                    ? ClipboardContentKind.Text
                    : ClipboardContentKind.Other;
            }

            return ClipboardContentKind.Empty;
        }
    }

    private static readonly string[] ImageFileExtensions = [".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tif", ".tiff", ".avif"];

    /// <summary>Whether <paramref name="path"/> is an image file directly or indirectly inside the temporary directory.</summary>
    public static bool IsTemporaryImageFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!ImageFileExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var temporary = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath())) + Path.DirectorySeparatorChar;
            var comparison = OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            return Path.GetFullPath(path).StartsWith(temporary, comparison);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or System.Security.SecurityException or PathTooLongException)
        {
            return false;
        }
    }

    /// <summary>Approximate memory footprint in bytes.</summary>
    public long ApproximateSize =>
        (Text?.Length ?? 0) * 2L
        + (Image?.LongLength ?? 0)
        + Files.Sum(f => f.Length * 2L)
        + RawFormats.Sum(f => f.Data.LongLength + f.Name.Length * 2L);

    /// <summary>
    /// A stable identity of the content that matters to the user. Two copies of the same text, file
    /// list or image have the same fingerprint even when volatile side formats (timestamps,
    /// source URLs and similar) differ.
    /// </summary>
    public string Fingerprint => fingerprint ??= ComputeFingerprint();

    /// <summary>Returns a copy that only contains plain text.</summary>
    public ClipboardSnapshot ToPlainText() => new() { Text = Text ?? string.Empty };

    private string ComputeFingerprint()
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        switch (Kind)
        {
            case ClipboardContentKind.File:
            case ClipboardContentKind.FileCollection:
                Append(hash, "files");
                foreach (var file in Files)
                {
                    Append(hash, file);
                }

                break;

            case ClipboardContentKind.Text when !string.IsNullOrEmpty(Text):
                Append(hash, "text");
                Append(hash, Text);
                break;

            case ClipboardContentKind.Image when Image is not null:
                Append(hash, "image");
                Append(hash, Image);
                break;

            default:
                Append(hash, "raw");
                foreach (var format in RawFormats)
                {
                    Append(hash, format.Name);
                    Append(hash, format.Data);
                }

                break;
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    // Every part is prefixed with its length so that no two different sequences of parts produce the same
    // bytes. Strings are hashed as their UTF-16 code units, which avoids copying large texts.
    private static void Append(IncrementalHash hash, string value) =>
        Append(hash, MemoryMarshal.AsBytes(value.AsSpan()));

    private static void Append(IncrementalHash hash, ReadOnlySpan<byte> value)
    {
        Span<byte> length = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(length, value.Length);
        hash.AppendData(length);
        hash.AppendData(value);
    }
}

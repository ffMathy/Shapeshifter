using System.Buffers.Binary;
using System.IO.Compression;

namespace Shapeshifter.Platform.Common;

/// <summary>Encodes 32-bit pixels as a PNG image, so native icons can be handed to the UI in a portable format.</summary>
internal static class PngEncoder
{
    private static readonly uint[] CrcTable = CreateCrcTable();

    /// <summary>Encodes straight (not premultiplied) BGRA pixels, top row first, with no padding between rows.</summary>
    public static byte[] EncodeBgra(ReadOnlySpan<byte> bgra, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (bgra.Length < width * height * 4)
        {
            throw new ArgumentException("The pixel buffer is smaller than width × height × 4.", nameof(bgra));
        }

        // Each row is prefixed with filter type 0 (none); PNG stores RGBA.
        var raw = new byte[height * ((width * 4) + 1)];
        var offset = 0;
        for (var y = 0; y < height; y++)
        {
            raw[offset++] = 0;
            var row = bgra.Slice(y * width * 4, width * 4);
            for (var x = 0; x < width; x++)
            {
                raw[offset++] = row[(x * 4) + 2];
                raw[offset++] = row[(x * 4) + 1];
                raw[offset++] = row[x * 4];
                raw[offset++] = row[(x * 4) + 3];
            }
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(raw);
        }

        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header[4..], height);
        header[8] = 8; // bit depth
        header[9] = 6; // colour type: truecolour with alpha
        header[10] = 0; // compression
        header[11] = 0; // filter
        header[12] = 0; // no interlace
        WriteChunk(png, "IHDR"u8, header);
        WriteChunk(png, "IDAT"u8, compressed.GetBuffer().AsSpan(0, (int)compressed.Length));
        WriteChunk(png, "IEND"u8, []);
        return png.ToArray();
    }

    private static void WriteChunk(Stream stream, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, data.Length);
        stream.Write(buffer);
        stream.Write(type);
        stream.Write(data);
        var crc = Update(Update(0xFFFFFFFFu, type), data) ^ 0xFFFFFFFFu;
        BinaryPrimitives.WriteUInt32BigEndian(buffer, crc);
        stream.Write(buffer);
    }

    private static uint Update(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
        {
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return crc;
    }

    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }
}

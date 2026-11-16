using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Shapeshifter.EndToEnd.TargetApp;

/// <summary>
/// Direct access to the Win32 clipboard, so tests can put exactly the formats real applications use on it
/// (and nothing that a UI toolkit would add or convert), and inspect every image format byte by byte.
/// </summary>
[SupportedOSPlatform("windows")]
internal static partial class WindowsClipboard
{
    public const uint CfBitmap = 2;
    public const uint CfDib = 8;
    public const uint CfHdrop = 15;
    public const uint CfDibV5 = 17;

    private const uint GmemMoveable = 0x0002;

    public static uint PngFormat => RegisterClipboardFormatW("PNG");

    /// <summary>Replaces the clipboard content with exactly <paramref name="formats"/>.</summary>
    public static void Set(IntPtr owner, IReadOnlyList<(uint Format, byte[] Data)> formats)
    {
        Open(owner);
        try
        {
            if (!EmptyClipboard())
            {
                throw new InvalidOperationException("EmptyClipboard failed: " + Marshal.GetLastPInvokeError());
            }

            foreach (var (format, data) in formats)
            {
                var memory = GlobalAlloc(GmemMoveable, (nuint)data.Length);
                var pointer = GlobalLock(memory);
                Marshal.Copy(data, 0, pointer, data.Length);
                GlobalUnlock(memory);
                if (SetClipboardData(format, memory) == IntPtr.Zero)
                {
                    throw new InvalidOperationException($"SetClipboardData({format}) failed: " + Marshal.GetLastPInvokeError());
                }
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    /// <summary>Describes every format on the clipboard, and decodes CF_DIB, CF_DIBV5 and PNG as pixels.</summary>
    public static string Describe(IntPtr owner, Func<byte[], string> decodePng)
    {
        var builder = new StringBuilder();
        Open(owner);
        try
        {
            var names = new List<string>();
            for (var format = EnumClipboardFormats(0); format != 0; format = EnumClipboardFormats(format))
            {
                names.Add(Name(format));
            }

            builder.Append("formats=").Append(string.Join(',', names));
            foreach (var (format, label) in new[] { (CfDib, "CF_DIB"), (CfDibV5, "CF_DIBV5"), (PngFormat, "PNG") })
            {
                builder.Append(';').Append(label).Append('=');
                var data = Read(format);
                if (data is null)
                {
                    builder.Append("absent");
                    continue;
                }

                try
                {
                    builder.Append(format == PngFormat ? decodePng(data) : DescribeDib(data));
                }
                catch (Exception exception)
                {
                    builder.Append("undecodable(").Append(exception.Message.Replace(';', ',')).Append(')');
                }
            }
        }
        finally
        {
            CloseClipboard();
        }

        return builder.ToString();
    }

    /// <summary>A 24-bit, bottom-up BI_RGB device-independent bitmap, as Snipping Tool and screenshot tools copy it.</summary>
    public static byte[] CreateDib24(int width, int height, uint[] argb)
    {
        var stride = (width * 3 + 3) & ~3;
        var bytes = new byte[40 + stride * height];
        WriteInfoHeader(bytes, width, height, bitCount: 24, compression: 0, imageSize: stride * height);
        for (var y = 0; y < height; y++)
        {
            var row = 40 + (height - 1 - y) * stride;
            for (var x = 0; x < width; x++)
            {
                var pixel = argb[y * width + x];
                bytes[row + x * 3] = (byte)pixel;
                bytes[row + x * 3 + 1] = (byte)(pixel >> 8);
                bytes[row + x * 3 + 2] = (byte)(pixel >> 16);
            }
        }

        return bytes;
    }

    /// <summary>The CF_HDROP (DROPFILES) structure for one file, with a wide-character path.</summary>
    public static byte[] CreateHdrop(string path)
    {
        var pathBytes = Encoding.Unicode.GetBytes(path + "\0\0");
        var bytes = new byte[20 + pathBytes.Length];
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0), 20);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(16), 1);
        pathBytes.CopyTo(bytes, 20);
        return bytes;
    }

    /// <summary>"WxH bpp=N compression=N pixels=AARRGGBB,..." (rows top to bottom). 24-bit pixels report alpha FF.</summary>
    public static string DescribeDib(byte[] dib)
    {
        var headerSize = BinaryPrimitives.ReadInt32LittleEndian(dib);
        var width = BinaryPrimitives.ReadInt32LittleEndian(dib.AsSpan(4));
        var height = BinaryPrimitives.ReadInt32LittleEndian(dib.AsSpan(8));
        var bitCount = BinaryPrimitives.ReadUInt16LittleEndian(dib.AsSpan(14));
        var compression = BinaryPrimitives.ReadUInt32LittleEndian(dib.AsSpan(16));
        var colorsUsed = BinaryPrimitives.ReadUInt32LittleEndian(dib.AsSpan(32));
        var bottomUp = height > 0;
        height = Math.Abs(height);

        // BI_BITFIELDS with a plain BITMAPINFOHEADER stores the three masks right after it.
        var offset = headerSize + (compression == 3 && headerSize == 40 ? 12 : 0);
        if (bitCount <= 8)
        {
            offset += (int)(colorsUsed == 0 ? 1u << bitCount : colorsUsed) * 4;
        }

        if (bitCount is not (24 or 32) || compression is not (0 or 3))
        {
            return $"{width}x{height} bpp={bitCount} compression={compression} pixels=unsupported";
        }

        var bytesPerPixel = bitCount / 8;
        var stride = (width * bytesPerPixel + 3) & ~3;
        var pixels = new List<string>(width * height);
        for (var y = 0; y < height; y++)
        {
            var row = offset + (bottomUp ? height - 1 - y : y) * stride;
            for (var x = 0; x < width; x++)
            {
                var p = row + x * bytesPerPixel;
                var alpha = bytesPerPixel == 4 ? dib[p + 3] : (byte)0xFF;
                pixels.Add(FormatPixel(alpha, dib[p + 2], dib[p + 1], dib[p]));
            }
        }

        return $"{width}x{height} bpp={bitCount} compression={compression} pixels={string.Join(',', pixels)}";
    }

    public static string FormatPixel(byte a, byte r, byte g, byte b) =>
        string.Create(CultureInfo.InvariantCulture, $"{a:X2}{r:X2}{g:X2}{b:X2}");

    private static void WriteInfoHeader(byte[] bytes, int width, int height, ushort bitCount, uint compression, int imageSize)
    {
        var span = bytes.AsSpan();
        BinaryPrimitives.WriteInt32LittleEndian(span, 40);
        BinaryPrimitives.WriteInt32LittleEndian(span[4..], width);
        BinaryPrimitives.WriteInt32LittleEndian(span[8..], height);
        BinaryPrimitives.WriteUInt16LittleEndian(span[12..], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(span[14..], bitCount);
        BinaryPrimitives.WriteUInt32LittleEndian(span[16..], compression);
        BinaryPrimitives.WriteInt32LittleEndian(span[20..], imageSize);
        BinaryPrimitives.WriteInt32LittleEndian(span[24..], 3780);
        BinaryPrimitives.WriteInt32LittleEndian(span[28..], 3780);
    }

    private static byte[]? Read(uint format)
    {
        if (!IsClipboardFormatAvailable(format))
        {
            return null;
        }

        var handle = GetClipboardData(format);
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        var size = (int)GlobalSize(handle);
        var pointer = GlobalLock(handle);
        try
        {
            var bytes = new byte[size];
            Marshal.Copy(pointer, bytes, 0, size);
            return bytes;
        }
        finally
        {
            GlobalUnlock(handle);
        }
    }

    private static string Name(uint format)
    {
        var buffer = new char[256];
        var length = GetClipboardFormatNameW(format, buffer, buffer.Length);
        return length > 0 ? new string(buffer, 0, length) : format.ToString(CultureInfo.InvariantCulture);
    }

    private static void Open(IntPtr owner)
    {
        // Another process (Shapeshifter itself) may be reading the clipboard right now.
        for (var attempt = 0; !OpenClipboard(owner); attempt++)
        {
            if (attempt == 50)
            {
                throw new InvalidOperationException("OpenClipboard failed: " + Marshal.GetLastPInvokeError());
            }

            Thread.Sleep(20);
        }
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenClipboard(IntPtr owner);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EmptyClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial IntPtr SetClipboardData(uint format, IntPtr memory);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetClipboardData(uint format);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsClipboardFormatAvailable(uint format);

    [LibraryImport("user32.dll")]
    private static partial uint EnumClipboardFormats(uint format);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint RegisterClipboardFormatW(string name);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int GetClipboardFormatNameW(uint format, [Out] char[] name, int maxCount);

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GlobalAlloc(uint flags, nuint bytes);

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GlobalLock(IntPtr memory);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalUnlock(IntPtr memory);

    [LibraryImport("kernel32.dll")]
    private static partial nuint GlobalSize(IntPtr memory);
}

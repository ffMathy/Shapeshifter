using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Serilog;
using Shapeshifter.Core.Platform;
using Shapeshifter.Platform.Common;

namespace Shapeshifter.Platform.Windows;

/// <summary>The application (and window) that owns the clipboard or is in the foreground.</summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsClipboardSourceLocator(ILogger logger) : IClipboardSourceLocator
{
    private const int MaxCachedEntries = 64;
    private const uint WindowIconTimeoutMilliseconds = 20;

    private readonly ILogger logger = logger;
    private readonly int ownProcessId = Environment.ProcessId;
    private readonly Dictionary<string, ExecutableInfo> executables = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string Path, nint Icon), byte[]?> windowIcons = [];
    private readonly Lock gate = new();

    public ClipboardSource? GetCurrentSource()
    {
        try
        {
            var window = ChooseWindow(out var processId);
            if (window == 0 || processId == 0 || processId == ownProcessId)
            {
                return null;
            }

            var path = GetProcessPath(processId);
            if (path is not null && IsApplicationFrameHost(path))
            {
                // A UWP app's frame belongs to ApplicationFrameHost; the app itself runs in a child window's process.
                var hostedProcessId = FindHostedProcess(window, processId);
                if (hostedProcessId != 0)
                {
                    path = GetProcessPath(hostedProcessId) ?? path;
                }
            }

            var title = WindowsNative.IsWindowVisible(window) ? GetWindowTitle(window) : null;
            var executable = path is null ? null : GetExecutableInfo(path);
            var icon = GetWindowIcon(window, path) ?? executable?.IconPng;
            var name = executable?.Name;
            if (name is null && title is null && icon is null)
            {
                return null;
            }

            return new ClipboardSource(name, title, icon);
        }
        catch (Exception exception)
        {
            logger.Debug(exception, "Could not determine which application the clipboard content came from");
            return null;
        }
    }

    /// <summary>
    /// Picks the window the copy most likely came from: the foreground window, unless the clipboard is owned by a
    /// visible window of another process (e.g. something that copied while in the background).
    /// </summary>
    private static nint ChooseWindow(out uint processId)
    {
        var foreground = WindowsNative.GetForegroundWindow();
        var owner = WindowsNative.GetClipboardOwner();
        var foregroundProcessId = GetProcessId(foreground);

        if (owner == 0 || !WindowsNative.IsWindowVisible(owner) || GetProcessId(owner) == foregroundProcessId)
        {
            if (foreground != 0)
            {
                processId = foregroundProcessId;
                return foreground;
            }

            if (owner == 0)
            {
                processId = 0;
                return 0;
            }
        }

        var root = WindowsNative.GetAncestor(owner, WindowsNative.GaRoot);
        var window = root != 0 ? root : owner;
        processId = GetProcessId(window);
        return window;
    }

    private static uint GetProcessId(nint window)
    {
        if (window == 0)
        {
            return 0;
        }

        _ = WindowsNative.GetWindowThreadProcessId(window, out var processId);
        return processId;
    }

    private static unsafe string? GetWindowTitle(nint window)
    {
        var length = WindowsNative.GetWindowTextLength(window);
        if (length <= 0)
        {
            return null;
        }

        length = Math.Min(length, 4096);
        var buffer = new char[length + 1];
        int copied;
        fixed (char* text = buffer)
        {
            copied = WindowsNative.GetWindowText(window, text, buffer.Length);
        }

        if (copied <= 0)
        {
            return null;
        }

        var title = new string(buffer, 0, Math.Min(copied, length)).Trim();
        return title.Length > 0 ? title : null;
    }

    private static unsafe string? GetProcessPath(uint processId)
    {
        var process = WindowsNative.OpenProcess(WindowsNative.ProcessQueryLimitedInformation, false, processId);
        if (process == 0)
        {
            return null;
        }

        try
        {
            var buffer = new char[1024];
            var size = (uint)buffer.Length;
            fixed (char* name = buffer)
            {
                if (!WindowsNative.QueryFullProcessImageName(process, 0, name, ref size) || size == 0)
                {
                    return null;
                }
            }

            return new string(buffer, 0, (int)Math.Min(size, (uint)buffer.Length));
        }
        finally
        {
            _ = WindowsNative.CloseHandle(process);
        }
    }

    private static bool IsApplicationFrameHost(string path) =>
        string.Equals(Path.GetFileName(path), "ApplicationFrameHost.exe", StringComparison.OrdinalIgnoreCase);

    private static unsafe uint FindHostedProcess(nint frame, uint hostProcessId)
    {
        var search = new HostedProcessSearch { HostProcessId = hostProcessId };
        _ = WindowsNative.EnumChildWindows(frame, &OnChildWindow, (nint)(&search));
        return search.FoundProcessId;
    }

    [UnmanagedCallersOnly]
    private static unsafe int OnChildWindow(nint window, nint state)
    {
        var search = (HostedProcessSearch*)state;
        _ = WindowsNative.GetWindowThreadProcessId(window, out var processId);
        if (processId != 0 && processId != search->HostProcessId)
        {
            search->FoundProcessId = processId;
            return 0; // stop enumerating
        }

        return 1;
    }

    private ExecutableInfo GetExecutableInfo(string path)
    {
        lock (gate)
        {
            if (executables.TryGetValue(path, out var cached))
            {
                return cached;
            }
        }

        var info = new ExecutableInfo(GetApplicationName(path), LoadExecutableIcon(path));
        lock (gate)
        {
            if (executables.Count >= MaxCachedEntries)
            {
                executables.Clear();
            }

            executables[path] = info;
        }

        return info;
    }

    private string? GetApplicationName(string path)
    {
        try
        {
            var version = FileVersionInfo.GetVersionInfo(path);
            if (!string.IsNullOrWhiteSpace(version.FileDescription))
            {
                return version.FileDescription.Trim();
            }

            if (!string.IsNullOrWhiteSpace(version.ProductName))
            {
                return version.ProductName.Trim();
            }
        }
        catch (Exception exception)
        {
            logger.Debug(exception, "Could not read the version information of {Path}", path);
        }

        var name = Path.GetFileNameWithoutExtension(path);
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private unsafe byte[]? LoadExecutableIcon(string path)
    {
        try
        {
            nint large = 0;
            nint small = 0;
            var result = WindowsNative.SHDefExtractIcon(path, 0, 0, &large, &small, 32 | (16u << 16));
            try
            {
                if (result == 0 && large != 0)
                {
                    var png = WindowsIcons.ToPng(large);
                    if (png is not null)
                    {
                        return png;
                    }
                }
            }
            finally
            {
                WindowsIcons.Destroy(large);
                WindowsIcons.Destroy(small);
            }

            var info = default(WindowsNative.ShFileInfo);
            var found = WindowsNative.SHGetFileInfo(
                path, 0, ref info, (uint)Unsafe.SizeOf<WindowsNative.ShFileInfo>(), WindowsNative.ShgfiIcon | WindowsNative.ShgfiLargeIcon);
            if (found == 0 || info.Icon == 0)
            {
                return null;
            }

            try
            {
                return WindowsIcons.ToPng(info.Icon);
            }
            finally
            {
                WindowsIcons.Destroy(info.Icon);
            }
        }
        catch (Exception exception)
        {
            logger.Debug(exception, "Could not extract the icon of {Path}", path);
            return null;
        }
    }

    /// <summary>The window's own big icon, which the window keeps ownership of (it must not be destroyed here).</summary>
    private byte[]? GetWindowIcon(nint window, string? path)
    {
        try
        {
            nint icon = 0;
            if (WindowsNative.SendMessageTimeout(
                    window,
                    WindowsNative.WmGetIcon,
                    WindowsNative.IconBig,
                    0,
                    WindowsNative.SmtoAbortIfHung,
                    WindowIconTimeoutMilliseconds,
                    out var result) != 0)
            {
                icon = result;
            }

            if (icon == 0)
            {
                icon = WindowsNative.GetClassLongPtr(window, WindowsNative.GclpHIcon);
            }

            if (icon == 0)
            {
                return null;
            }

            var key = (path ?? string.Empty, icon);
            lock (gate)
            {
                if (windowIcons.TryGetValue(key, out var cached))
                {
                    return cached;
                }
            }

            var png = WindowsIcons.ToPng(icon);
            lock (gate)
            {
                if (windowIcons.Count >= MaxCachedEntries)
                {
                    windowIcons.Clear();
                }

                windowIcons[key] = png;
            }

            return png;
        }
        catch (Exception exception)
        {
            logger.Debug(exception, "Could not read the icon of window {Window}", window);
            return null;
        }
    }

    private sealed record ExecutableInfo(string? Name, byte[]? IconPng);

    private struct HostedProcessSearch
    {
        public uint HostProcessId;
        public uint FoundProcessId;
    }
}

/// <summary>Shell icons for files and folders, as Explorer shows them.</summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsFileIconProvider(ILogger logger) : IFileIconProvider
{
    private const int MaxCachedEntries = 256;
    private const string FolderKey = "<folder>";

    /// <summary>Files whose icon is stored in (or points to) the file itself, so it differs per file.</summary>
    private static readonly HashSet<string> PerPathExtensions = new(StringComparer.OrdinalIgnoreCase) { ".exe", ".lnk", ".ico" };

    private readonly ILogger logger = logger;
    private readonly Dictionary<string, byte[]?> cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock gate = new();

    public byte[]? GetIconPng(string path, int size)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            var isDirectory = FileSystemProbe.IsDirectory(path);
            var extension = isDirectory ? string.Empty : Path.GetExtension(path);
            var perPath = !isDirectory && PerPathExtensions.Contains(extension);
            var imageList = size <= 16 ? WindowsNative.ShilSmall : size <= 48 ? WindowsNative.ShilExtraLarge : WindowsNative.ShilJumbo;
            var key = string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{imageList}|{(isDirectory ? FolderKey : perPath ? path : extension)}");

            lock (gate)
            {
                if (cache.TryGetValue(key, out var cached))
                {
                    return cached;
                }
            }

            var useRealFile = perPath && FileSystemProbe.IsLocalFile(path);
            var shellPath = useRealFile ? path : isDirectory ? "folder" : "file" + extension;
            var png = LoadIcon(shellPath, isDirectory, useRealFile, imageList, size);

            lock (gate)
            {
                if (cache.Count >= MaxCachedEntries)
                {
                    cache.Clear();
                }

                cache[key] = png;
            }

            return png;
        }
        catch (Exception exception)
        {
            logger.Debug(exception, "Could not get the shell icon of {Path}", path);
            return null;
        }
    }

    private static byte[]? LoadIcon(string shellPath, bool isDirectory, bool useRealFile, int imageList, int size)
    {
        uint attributes = 0;
        uint flags = 0;
        if (!useRealFile)
        {
            flags |= WindowsNative.ShgfiUseFileAttributes;
            attributes = isDirectory ? WindowsNative.FileAttributeDirectory : WindowsNative.FileAttributeNormal;
        }

        var infoSize = (uint)Unsafe.SizeOf<WindowsNative.ShFileInfo>();
        var info = default(WindowsNative.ShFileInfo);
        if (WindowsNative.SHGetFileInfo(shellPath, attributes, ref info, infoSize, flags | WindowsNative.ShgfiSysIconIndex) != 0)
        {
            var png = FromSystemImageList(imageList, info.IconIndex);
            if (png is not null)
            {
                return png;
            }
        }

        // Fall back to the small or large icon SHGetFileInfo hands out directly.
        info = default;
        var sizeFlag = size <= 16 ? WindowsNative.ShgfiSmallIcon : WindowsNative.ShgfiLargeIcon;
        if (WindowsNative.SHGetFileInfo(shellPath, attributes, ref info, infoSize, flags | WindowsNative.ShgfiIcon | sizeFlag) == 0
            || info.Icon == 0)
        {
            return null;
        }

        try
        {
            return WindowsIcons.ToPng(info.Icon);
        }
        finally
        {
            WindowsIcons.Destroy(info.Icon);
        }
    }

    private static unsafe byte[]? FromSystemImageList(int imageList, int index)
    {
        if (index < 0
            || WindowsNative.SHGetImageList(imageList, in WindowsNative.ImageListInterfaceId, out var list) < 0
            || list == 0)
        {
            return null;
        }

        nint icon = 0;
        try
        {
            // IImageList::GetIcon is the 8th method after IUnknown's three (vtable slot 10).
            var vtable = *(nint**)list;
            var getIcon = (delegate* unmanaged[Stdcall]<nint, int, uint, nint*, int>)vtable[10];
            if (getIcon(list, index, WindowsNative.IldTransparent, &icon) < 0 || icon == 0)
            {
                return null;
            }

            return WindowsIcons.ToPng(icon, cropSmallJumbo: imageList == WindowsNative.ShilJumbo);
        }
        finally
        {
            WindowsIcons.Destroy(icon);
            _ = Marshal.Release(list);
        }
    }
}

/// <summary>Converts Windows icons (HICON) to PNG images.</summary>
[SupportedOSPlatform("windows")]
internal static class WindowsIcons
{
    private const int MaxIconSize = 512;
    private const int JumboFallbackSize = 48;

    public static void Destroy(nint icon)
    {
        if (icon != 0)
        {
            _ = WindowsNative.DestroyIcon(icon);
        }
    }

    /// <summary>
    /// Encodes <paramref name="icon"/> as a PNG. The icon itself is not destroyed. With
    /// <paramref name="cropSmallJumbo"/>, a 256 px image that only holds a 48 px icon in its corner (as the jumbo
    /// image list produces for files without a large icon) is cropped to that corner.
    /// </summary>
    public static byte[]? ToPng(nint icon, bool cropSmallJumbo = false)
    {
        var pixels = ToBgra(icon, out var width, out var height);
        if (pixels is null)
        {
            return null;
        }

        if (cropSmallJumbo && width > JumboFallbackSize && height > JumboFallbackSize
            && IsTransparentOutside(pixels, width, height, JumboFallbackSize, JumboFallbackSize))
        {
            pixels = Crop(pixels, width, JumboFallbackSize, JumboFallbackSize);
            width = height = JumboFallbackSize;
        }

        return PngEncoder.EncodeBgra(pixels, width, height);
    }

    /// <summary>Reads the icon as straight BGRA pixels, top row first.</summary>
    private static byte[]? ToBgra(nint icon, out int width, out int height)
    {
        width = height = 0;
        if (!WindowsNative.GetIconInfo(icon, out var info))
        {
            return null;
        }

        var dc = WindowsNative.CreateCompatibleDC(0);
        try
        {
            if (dc == 0 || info.MaskBitmap == 0)
            {
                return null;
            }

            if (info.ColorBitmap == 0)
            {
                return ReadMonochrome(dc, info.MaskBitmap, out width, out height);
            }

            if (!TryGetSize(info.ColorBitmap, out width, out height))
            {
                return null;
            }

            var color = ReadBitmap(dc, info.ColorBitmap, width, height);
            if (color is null)
            {
                return null;
            }

            if (!HasAnyAlpha(color))
            {
                // An icon without an alpha channel: its transparency is in the AND mask.
                var mask = ReadBitmap(dc, info.MaskBitmap, width, height);
                if (mask is null)
                {
                    return null;
                }

                ApplyMaskAlpha(color, mask);
            }

            return color;
        }
        finally
        {
            if (dc != 0)
            {
                _ = WindowsNative.DeleteDC(dc);
            }

            if (info.ColorBitmap != 0)
            {
                _ = WindowsNative.DeleteObject(info.ColorBitmap);
            }

            if (info.MaskBitmap != 0)
            {
                _ = WindowsNative.DeleteObject(info.MaskBitmap);
            }
        }
    }

    /// <summary>A monochrome icon's mask is twice its height: the AND mask on top, the XOR mask below.</summary>
    private static byte[]? ReadMonochrome(nint dc, nint maskBitmap, out int width, out int height)
    {
        if (!TryGetSize(maskBitmap, out width, out var maskHeight) || maskHeight < 2)
        {
            height = 0;
            return null;
        }

        height = maskHeight / 2;
        var mask = ReadBitmap(dc, maskBitmap, width, maskHeight);
        if (mask is null)
        {
            return null;
        }

        var pixels = new byte[width * height * 4];
        var xorOffset = width * height * 4;
        for (var i = 0; i < pixels.Length; i += 4)
        {
            var transparent = mask[i] != 0;
            var white = mask[xorOffset + i] != 0;
            var value = (byte)(white ? 255 : 0);
            pixels[i] = pixels[i + 1] = pixels[i + 2] = value;
            pixels[i + 3] = (byte)(transparent ? 0 : 255);
        }

        return pixels;
    }

    private static bool TryGetSize(nint bitmap, out int width, out int height)
    {
        width = height = 0;
        if (WindowsNative.GetObject(bitmap, Unsafe.SizeOf<WindowsNative.Bitmap>(), out var info) == 0)
        {
            return false;
        }

        width = info.Width;
        height = Math.Abs(info.Height);
        return width > 0 && height > 0 && width <= MaxIconSize && height <= MaxIconSize * 2;
    }

    /// <summary>Reads a bitmap as 32 bits per pixel, top row first.</summary>
    private static unsafe byte[]? ReadBitmap(nint dc, nint bitmap, int width, int height)
    {
        var pixels = new byte[width * height * 4];

        // Room for a colour table too, which GetDIBits may write for palette-based bitmaps.
        var infoSize = sizeof(WindowsNative.BitmapInfoHeader) + (256 * 4);
        var info = stackalloc byte[infoSize];
        new Span<byte>(info, infoSize).Clear();
        var header = (WindowsNative.BitmapInfoHeader*)info;
        header->Size = (uint)sizeof(WindowsNative.BitmapInfoHeader);
        header->Width = width;
        header->Height = -height; // top-down
        header->Planes = 1;
        header->BitCount = 32;
        header->Compression = WindowsNative.BiRgb;

        int lines;
        fixed (byte* bits = pixels)
        {
            lines = WindowsNative.GetDIBits(dc, bitmap, 0, (uint)height, bits, info, WindowsNative.DibRgbColors);
        }

        return lines == height ? pixels : null;
    }

    private static bool HasAnyAlpha(ReadOnlySpan<byte> bgra)
    {
        for (var i = 3; i < bgra.Length; i += 4)
        {
            if (bgra[i] != 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Makes pixels whose AND-mask bit is set (white in a 32 bpp read of the mask) transparent, the rest opaque.</summary>
    private static void ApplyMaskAlpha(Span<byte> bgra, ReadOnlySpan<byte> mask)
    {
        var length = Math.Min(bgra.Length, mask.Length);
        for (var i = 0; i + 3 < length; i += 4)
        {
            var transparent = mask[i] != 0 || mask[i + 1] != 0 || mask[i + 2] != 0;
            bgra[i + 3] = (byte)(transparent ? 0 : 255);
        }
    }

    private static bool IsTransparentOutside(ReadOnlySpan<byte> bgra, int width, int height, int keepWidth, int keepHeight)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = y < keepHeight ? keepWidth : 0; x < width; x++)
            {
                if (bgra[(((y * width) + x) * 4) + 3] != 0)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static byte[] Crop(ReadOnlySpan<byte> bgra, int width, int cropWidth, int cropHeight)
    {
        var result = new byte[cropWidth * cropHeight * 4];
        for (var y = 0; y < cropHeight; y++)
        {
            bgra.Slice(y * width * 4, cropWidth * 4).CopyTo(result.AsSpan(y * cropWidth * 4));
        }

        return result;
    }
}

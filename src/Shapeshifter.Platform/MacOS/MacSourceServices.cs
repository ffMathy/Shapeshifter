using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Serilog;
using Shapeshifter.Core.Platform;
using Shapeshifter.Platform.Common;

namespace Shapeshifter.Platform.MacOS;

/// <summary>The frontmost application (and its front window's title, when macOS allows it).</summary>
/// <remarks>
/// Window titles come from <c>CGWindowListCopyWindowInfo</c>, which only includes <c>kCGWindowName</c> when the user
/// has granted Shapeshifter the Screen Recording permission; without it the application name is shown instead.
/// </remarks>
[SupportedOSPlatform("macos")]
internal sealed class MacClipboardSourceLocator(ILogger logger) : IClipboardSourceLocator
{
    private const int IconPixels = 64;
    private const int MaxCachedIcons = 64;

    private const uint WindowListOptionOnScreenOnly = 1 << 0;
    private const uint WindowListExcludeDesktopElements = 1 << 4;
    private const uint NullWindowId = 0;
    private const nint NumberSInt32Type = 3;

    private readonly ILogger logger = logger;
    private readonly Dictionary<string, byte[]?> iconsByBundleId = new(StringComparer.Ordinal);
    private readonly Lock iconLock = new();
    private readonly Lazy<WindowInfoKeys?> windowInfoKeys = new(WindowInfoKeys.Load);

    public ClipboardSource? GetCurrentSource()
    {
        try
        {
            if (!MacNative.TryLoadAppKit())
            {
                return null;
            }

            var pool = MacNative.objc_autoreleasePoolPush();
            try
            {
                return GetSourceInPool();
            }
            finally
            {
                MacNative.objc_autoreleasePoolPop(pool);
            }
        }
        catch (Exception exception)
        {
            logger.Debug(exception, "Finding the frontmost application failed");
            return null;
        }
    }

    private ClipboardSource? GetSourceInPool()
    {
        var workspaceClass = MacNative.objc_getClass("NSWorkspace");
        if (workspaceClass == IntPtr.Zero)
        {
            return null;
        }

        var workspace = MacNative.SendIntPtr(workspaceClass, MacNative.sel_registerName("sharedWorkspace"));
        if (workspace == IntPtr.Zero)
        {
            return null;
        }

        var application = MacNative.SendIntPtr(workspace, MacNative.sel_registerName("frontmostApplication"));
        if (application == IntPtr.Zero)
        {
            return null;
        }

        var pid = MacNative.SendInt32(application, MacNative.sel_registerName("processIdentifier"));
        if (pid <= 0 || pid == Environment.ProcessId)
        {
            return null;
        }

        var name = MacNative.ToManagedString(MacNative.SendIntPtr(application, MacNative.sel_registerName("localizedName")));
        var bundleId = MacNative.ToManagedString(MacNative.SendIntPtr(application, MacNative.sel_registerName("bundleIdentifier")));
        var icon = GetApplicationIcon(application, bundleId);
        var title = GetFrontWindowTitle(pid);

        return name is null && title is null && icon is null ? null : new ClipboardSource(name, title, icon);
    }

    private byte[]? GetApplicationIcon(IntPtr application, string? bundleId)
    {
        if (bundleId is not null)
        {
            lock (iconLock)
            {
                if (iconsByBundleId.TryGetValue(bundleId, out var cached))
                {
                    return cached;
                }
            }
        }

        var image = MacNative.SendIntPtr(application, MacNative.sel_registerName("icon"));
        var png = MacImageRenderer.RenderPng(image, IconPixels);

        if (bundleId is not null)
        {
            lock (iconLock)
            {
                if (iconsByBundleId.Count >= MaxCachedIcons)
                {
                    iconsByBundleId.Clear();
                }

                iconsByBundleId[bundleId] = png;
            }
        }

        return png;
    }

    /// <summary>The title of <paramref name="pid"/>'s frontmost normal (layer 0) window, or null when unavailable.</summary>
    private string? GetFrontWindowTitle(int pid)
    {
        var keys = windowInfoKeys.Value;
        if (keys is null)
        {
            return null;
        }

        var windows = MacNative.CGWindowListCopyWindowInfo(WindowListOptionOnScreenOnly | WindowListExcludeDesktopElements, NullWindowId);
        if (windows == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var numberType = MacNative.CFNumberGetTypeID();
            var stringType = MacNative.CFStringGetTypeID();
            var count = MacNative.CFArrayGetCount(windows);

            // The list is ordered front to back, so the first matching window is the frontmost one.
            for (nint index = 0; index < count; index++)
            {
                var window = MacNative.CFArrayGetValueAtIndex(windows, index);
                if (window == IntPtr.Zero
                    || !TryGetInt32(window, keys.OwnerPid, numberType, out var owner) || owner != pid
                    || !TryGetInt32(window, keys.Layer, numberType, out var layer) || layer != 0)
                {
                    continue;
                }

                var name = MacNative.CFDictionaryGetValue(window, keys.Name);
                if (name == IntPtr.Zero || MacNative.CFGetTypeID(name) != stringType)
                {
                    continue;
                }

                var title = MacNative.ToManagedString(name);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }

            return null;
        }
        finally
        {
            MacNative.CFRelease(windows);
        }
    }

    private static bool TryGetInt32(IntPtr dictionary, IntPtr key, nuint numberType, out int value)
    {
        value = 0;
        var number = MacNative.CFDictionaryGetValue(dictionary, key);
        return number != IntPtr.Zero
            && MacNative.CFGetTypeID(number) == numberType
            && MacNative.CFNumberGetValue(number, NumberSInt32Type, out value) != 0;
    }

    /// <summary>The <c>kCGWindow…</c> dictionary keys, read from Core Graphics' exported constants.</summary>
    private sealed record WindowInfoKeys(IntPtr OwnerPid, IntPtr Layer, IntPtr Name)
    {
        public static WindowInfoKeys? Load()
        {
            var ownerPid = MacNative.GetCoreGraphicsStringConstant("kCGWindowOwnerPID");
            var layer = MacNative.GetCoreGraphicsStringConstant("kCGWindowLayer");
            var name = MacNative.GetCoreGraphicsStringConstant("kCGWindowName");
            return ownerPid == IntPtr.Zero || layer == IntPtr.Zero || name == IntPtr.Zero
                ? null
                : new WindowInfoKeys(ownerPid, layer, name);
        }
    }
}

/// <summary>File and folder icons as Finder shows them.</summary>
/// <remarks>
/// Regular files get the generic icon for their extension (so icons can be cached per extension); plain folders get
/// the standard folder icon. Existing directories with an extension (bundles such as <c>.app</c>) get their own icon.
/// </remarks>
[SupportedOSPlatform("macos")]
internal sealed class MacFileIconProvider(ILogger logger) : IFileIconProvider
{
    private const int MaxCachedIcons = 256;
    private const string FolderType = "public.folder";
    private const string GenericFileType = "public.data";

    private readonly ILogger logger = logger;
    private readonly Dictionary<string, byte[]?> cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock cacheLock = new();

    public byte[]? GetIconPng(string path, int size)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            // Render at twice the requested size for Retina displays.
            var pixels = Math.Clamp(size * 2, 16, 512);
            var (cacheKey, lookup, isFileType) = Classify(path);
            var key = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{pixels}|{cacheKey}");

            lock (cacheLock)
            {
                if (cache.TryGetValue(key, out var cached))
                {
                    return cached;
                }
            }

            var png = Render(lookup, isFileType, pixels);

            lock (cacheLock)
            {
                if (cache.Count >= MaxCachedIcons)
                {
                    cache.Clear();
                }

                cache[key] = png;
            }

            return png;
        }
        catch (Exception exception)
        {
            logger.Debug(exception, "Getting the icon for {Path} failed", path);
            return null;
        }
    }

    /// <summary>
    /// Decides how to look the icon up: by file type (an extension or a UTI, cached by type) or, for bundles, by the
    /// path itself (cached by path).
    /// </summary>
    private static (string CacheKey, string Lookup, bool IsFileType) Classify(string path)
    {
        var trimmed = Path.TrimEndingDirectorySeparator(path);
        var isDirectory = FileSystemProbe.IsDirectory(path);
        var extension = Path.GetExtension(trimmed).TrimStart('.');

        if (isDirectory)
        {
            return extension.Length > 0 && !FileSystemProbe.IsNetworkPath(trimmed) && Directory.Exists(trimmed)
                ? ("path:" + trimmed, trimmed, false)
                : ("folder", FolderType, true);
        }

        return extension.Length > 0
            ? ("ext:" + extension, extension, true)
            : ("file", GenericFileType, true);
    }

    private static byte[]? Render(string lookup, bool isFileType, int pixels)
    {
        if (!MacNative.TryLoadAppKit())
        {
            return null;
        }

        var pool = MacNative.objc_autoreleasePoolPush();
        try
        {
            var workspaceClass = MacNative.objc_getClass("NSWorkspace");
            if (workspaceClass == IntPtr.Zero)
            {
                return null;
            }

            var workspace = MacNative.SendIntPtr(workspaceClass, MacNative.sel_registerName("sharedWorkspace"));
            var argument = MacNative.CreateNSString(lookup);
            if (workspace == IntPtr.Zero || argument == IntPtr.Zero)
            {
                return null;
            }

            // iconForFileType: is deprecated since macOS 12 but still works, and accepts both extensions and UTIs.
            var selector = MacNative.sel_registerName(isFileType ? "iconForFileType:" : "iconForFile:");
            var image = MacNative.SendIntPtrIntPtr(workspace, selector, argument);
            return MacImageRenderer.RenderPng(image, pixels);
        }
        finally
        {
            MacNative.objc_autoreleasePoolPop(pool);
        }
    }
}

/// <summary>Rasterises an <c>NSImage</c> into a square PNG via Core Graphics.</summary>
[SupportedOSPlatform("macos")]
internal static class MacImageRenderer
{
    private const uint ImageAlphaPremultipliedFirst = 2;
    private const uint BitmapByteOrder32Little = 2 << 12;
    private const int InterpolationQualityHigh = 3;

    /// <summary>
    /// Draws <paramref name="nsImage"/> centred and aspect-fitted into a <paramref name="pixels"/>² bitmap and encodes it
    /// as PNG. Must be called inside an autorelease pool. Returns null for a null image or on failure.
    /// </summary>
    public static byte[]? RenderPng(IntPtr nsImage, int pixels)
    {
        if (nsImage == IntPtr.Zero)
        {
            return null;
        }

        // Asks the image for its best representation at this size; the returned CGImage is not owned by us.
        var proposed = new MacNative.CGRect { Width = pixels, Height = pixels };
        var cgImage = MacNative.SendCGImageForProposedRect(
            nsImage, MacNative.sel_registerName("CGImageForProposedRect:context:hints:"), ref proposed, IntPtr.Zero, IntPtr.Zero);
        if (cgImage == IntPtr.Zero)
        {
            return null;
        }

        double imageWidth = MacNative.CGImageGetWidth(cgImage);
        double imageHeight = MacNative.CGImageGetHeight(cgImage);
        if (imageWidth <= 0 || imageHeight <= 0)
        {
            return null;
        }

        var scale = Math.Min(pixels / imageWidth, pixels / imageHeight);
        var drawWidth = imageWidth * scale;
        var drawHeight = imageHeight * scale;
        var target = new MacNative.CGRect
        {
            X = (pixels - drawWidth) / 2,
            Y = (pixels - drawHeight) / 2,
            Width = drawWidth,
            Height = drawHeight,
        };

        // Pinned and zeroed, so Core Graphics can draw straight into it and transparent areas stay transparent.
        var stride = pixels * 4;
        var buffer = GC.AllocateArray<byte>(stride * pixels, pinned: true);

        var colorSpace = MacNative.CGColorSpaceCreateDeviceRGB();
        if (colorSpace == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            // Premultiplied alpha first, 32-bit little endian: bytes in memory are B, G, R, A, top row first.
            var context = MacNative.CGBitmapContextCreate(
                Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0),
                (nuint)pixels,
                (nuint)pixels,
                8,
                (nuint)stride,
                colorSpace,
                ImageAlphaPremultipliedFirst | BitmapByteOrder32Little);
            if (context == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                MacNative.CGContextSetInterpolationQuality(context, InterpolationQualityHigh);
                MacNative.CGContextDrawImage(context, target, cgImage);
            }
            finally
            {
                MacNative.CGContextRelease(context);
            }
        }
        finally
        {
            MacNative.CGColorSpaceRelease(colorSpace);
        }

        Unpremultiply(buffer);
        return PngEncoder.EncodeBgra(buffer, pixels, pixels);
    }

    private static void Unpremultiply(Span<byte> bgra)
    {
        for (var i = 0; i < bgra.Length; i += 4)
        {
            var alpha = bgra[i + 3];
            if (alpha is 0 or 255)
            {
                continue;
            }

            for (var channel = 0; channel < 3; channel++)
            {
                bgra[i + channel] = (byte)Math.Min(255, ((bgra[i + channel] * 255) + (alpha / 2)) / alpha);
            }
        }
    }
}

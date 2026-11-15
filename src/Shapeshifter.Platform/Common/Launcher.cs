using System.Diagnostics;
using Serilog;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Platform.Common;

/// <summary>Opens URLs and folders with the desktop's default handler (ShellExecute, <c>open</c> or <c>xdg-open</c>).</summary>
public sealed class Launcher(ILogger logger) : ILauncher
{
    public void OpenUrl(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Only web links can be opened.", nameof(uri));
        }

        Open(uri.AbsoluteUri);
    }

    public void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Open(path);
    }

    private void Open(string target)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            logger.Warning(exception, "Could not open {Target}", target);
        }
    }
}

namespace Shapeshifter.Desktop;

/// <summary>Ensures one Shapeshifter per user by holding an exclusive lock on a file.</summary>
internal static class SingleInstance
{
    public static IDisposable? TryAcquire(string lockFile)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(lockFile)!);
            var stream = new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
            stream.SetLength(0);
            var pid = System.Text.Encoding.ASCII.GetBytes(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            stream.Write(pid);
            stream.Flush();
            return stream;
        }
        catch (IOException)
        {
            return null;
        }
    }
}

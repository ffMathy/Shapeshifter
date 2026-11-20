using System.Runtime.InteropServices;

namespace Shapeshifter.Core.Updates;

/// <summary>
/// Whether an app store installed this copy: the Microsoft Store (an MSIX package) or the Mac App Store (a sandboxed
/// app). The store installs and updates such a copy, so it never installs or updates itself, and it cannot write the
/// usual start-at-login entries: the package (or the sandbox) keeps them to itself, where Windows and launchd never see them.
/// </summary>
public static class StoreInstall
{
    // GetCurrentPackageFullName: ERROR_INSUFFICIENT_BUFFER when the process has a package identity (the buffer of
    // length 0 is too small for its name), APPMODEL_ERROR_NO_PACKAGE when it has none.
    private const int ErrorInsufficientBuffer = 122;

    private static readonly Lazy<bool> Detected = new(Detect);

    /// <summary>True when the Microsoft Store or the Mac App Store installed this copy.</summary>
    public static bool IsActive => Detected.Value;

    private static bool Detect()
    {
        if (OperatingSystem.IsWindows())
        {
            uint length = 0;
            return GetCurrentPackageFullName(ref length, IntPtr.Zero) == ErrorInsufficientBuffer;
        }

        // macOS sets this for every process that runs in the App Sandbox, which the Mac App Store requires.
        return OperatingSystem.IsMacOS() && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APP_SANDBOX_CONTAINER_ID"));
    }

    // DllImport rather than LibraryImport: the signature is blittable, and the generated stub would need unsafe code.
    [DllImport("kernel32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int GetCurrentPackageFullName(ref uint packageFullNameLength, IntPtr packageFullName);
}

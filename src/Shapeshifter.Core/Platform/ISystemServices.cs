namespace Shapeshifter.Core.Platform;

/// <summary>Registers the application to start when the user logs in.</summary>
public interface IAutostartService
{
    bool IsSupported { get; }

    bool IsEnabled { get; }

    void SetEnabled(bool enabled);
}

/// <summary>Opens URLs and folders with the user's default handlers.</summary>
public interface ILauncher
{
    void OpenUrl(Uri uri);

    void OpenFolder(string path);
}

/// <summary>Where the mouse pointer is, in physical screen pixels, if the platform can tell.</summary>
public interface ICursorLocator
{
    (int X, int Y)? GetCursorPosition();
}

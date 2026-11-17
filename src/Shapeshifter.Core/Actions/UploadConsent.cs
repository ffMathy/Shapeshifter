using Shapeshifter.Core.Settings;

namespace Shapeshifter.Core.Actions;

/// <summary>The answer to whether content may be uploaded to an online service.</summary>
public enum UploadConsentResult
{
    /// <summary>The user did not agree; nothing may be uploaded.</summary>
    Denied,

    /// <summary>The user agreed on an earlier occasion; nothing was shown.</summary>
    GrantedEarlier,

    /// <summary>The user agreed just now, in a dialog that took the keyboard focus.</summary>
    GrantedNow,
}

/// <summary>Makes sure the user has agreed to upload content to an online service before anything is uploaded.</summary>
public interface IUploadConsent
{
    Task<UploadConsentResult> RequestAsync(string serviceName, CancellationToken cancellationToken = default);
}

/// <summary>Never agrees. Used where no one can be asked.</summary>
public sealed class NoUploadConsent : IUploadConsent
{
    public static NoUploadConsent Instance { get; } = new();

    public Task<UploadConsentResult> RequestAsync(string serviceName, CancellationToken cancellationToken = default) =>
        Task.FromResult(UploadConsentResult.Denied);
}

/// <summary>
/// Asks the user the first time and remembers a "yes" in the settings, so they are asked only once. A "no" is not
/// remembered: the user is asked again the next time they choose to upload.
/// </summary>
public sealed class RememberedUploadConsent(SettingsStore settings, Func<string, CancellationToken, Task<bool>> ask) : IUploadConsent, IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);

    public void Dispose() => gate.Dispose();

    public async Task<UploadConsentResult> RequestAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceName);

        // One question at a time, so two uploads in quick succession do not open two dialogs.
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (settings.Current.UploadConsents.Contains(serviceName, StringComparer.OrdinalIgnoreCase))
            {
                return UploadConsentResult.GrantedEarlier;
            }

            if (!await ask(serviceName, cancellationToken).ConfigureAwait(false))
            {
                return UploadConsentResult.Denied;
            }

            settings.Update(current => current with { UploadConsents = [.. current.UploadConsents, serviceName] });
            return UploadConsentResult.GrantedNow;
        }
        finally
        {
            gate.Release();
        }
    }
}

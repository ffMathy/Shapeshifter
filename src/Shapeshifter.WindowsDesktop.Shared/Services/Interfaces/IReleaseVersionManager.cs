namespace Shapeshifter.WindowsDesktop.Shared.Services.Interfaces
{
    using System;

    public interface IReleaseVersionManager
    {
        Version GetCurrentVersion();

        void SetCurrentVersion(Version version);
    }
}
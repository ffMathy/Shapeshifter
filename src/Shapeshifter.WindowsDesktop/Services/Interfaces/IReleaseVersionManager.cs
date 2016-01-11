namespace Shapeshifter.WindowsDesktop.Services.Interfaces
{
    using System;

    public interface IReleaseVersionManager
    {
        Version GetCurrentVersion();

        void SetCurrentVersion(Version version);
    }
}
namespace Shapeshifter.WindowsDesktop.Infrastructure.Events
{
    using System;

    using Data.Interfaces;

    public class PackageEventArgument: EventArgs
    {
        public PackageEventArgument(IClipboardDataControlPackage package)
        {
            Package = package;
        }

        public IClipboardDataControlPackage Package { get; }
    }
}
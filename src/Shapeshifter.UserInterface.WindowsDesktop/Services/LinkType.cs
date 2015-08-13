using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    [Flags]
    public enum LinkType
    {
        Https,
        Http,
        ImageFile,
        TextFile
    }
}

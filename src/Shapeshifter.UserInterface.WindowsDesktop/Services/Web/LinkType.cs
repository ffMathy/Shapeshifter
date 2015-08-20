using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    [Flags]
    public enum LinkType
    {
        NoType,
        Https,
        Http,
        ImageFile
    }
}

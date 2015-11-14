using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Web
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
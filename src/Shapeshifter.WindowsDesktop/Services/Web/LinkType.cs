namespace Shapeshifter.WindowsDesktop.Services.Web
{
    using System;

    [Flags]
    public enum LinkType
    {
        NoType,

        Https,

        Http,

        ImageFile
    }
}
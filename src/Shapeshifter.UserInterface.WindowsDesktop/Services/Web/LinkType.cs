#region

using System;

#endregion

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
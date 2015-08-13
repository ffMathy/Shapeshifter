using System.Collections.Generic;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface ILinkParser
    {
        IEnumerable<string> ExtractLinksFromText(string text);

        bool HasLink(string text);

        bool HasLinkOfType(string text, LinkType linkType);

        bool IsValidLink(string link);

        LinkType GetLinkType(string link);
    }
}

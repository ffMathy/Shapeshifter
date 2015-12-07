namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Infrastructure.Dependencies.Interfaces;

    public interface ILinkParser: ISingleInstance
    {
        Task<IReadOnlyCollection<string>> ExtractLinksFromTextAsync(string text);

        Task<bool> HasLinkAsync(string text);

        Task<bool> HasLinkOfTypeAsync(string text, LinkType linkType);

        Task<bool> IsValidLinkAsync(string link);

        bool IsLinkOfType(string link, LinkType type);

        LinkType GetLinkType(string link);
    }
}
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    class LinkParser : ILinkParser
    {
        static readonly Regex linkValidationExpression;
        static readonly Regex whitespaceExpression;

        readonly IDomainNameResolver domainNameResolver;
        readonly IFileTypeInterpreter fileTypeInterpreter;

        static LinkParser()
        {
            linkValidationExpression = new Regex(@"^(?:(?:https?|ftp):\/\/)?((?:\S+(?::\S*)?@)?(?:(?!10(?:\.\d{1,3}){3})(?!127(?:\.\d{1,3}){3})(?!169\.254(?:\.\d{1,3}){2})(?!192\.168(?:\.\d{1,3}){2})(?!172\.(?:1[6-9]|2\d|3[0-1])(?:\.\d{1,3}){2})(?:[1-9]\d?|1\d\d|2[01]\d|22[0-3])(?:\.(?:1?\d{1,2}|2[0-4]\d|25[0-5])){2}(?:\.(?:[1-9]\d?|1\d\d|2[0-4]\d|25[0-4]))|(?:(?:[a-z\u00A1-\uFFFF0-9]+-?)*[a-z\u00A1-\uFFFF0-9]+)(?:\.(?:[a-z\u00A1-\uFFFF0-9]+-?)*[a-z\u00A1-\uFFFF0-9]+)*(?:\.(?:[a-z\u00A1-\uFFFF]{2,})))(?::\d{2,5})?)(?:\/?[^\s]*)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            whitespaceExpression = new Regex(@"\s", RegexOptions.Compiled);
        }

        public LinkParser(
            IFileTypeInterpreter fileTypeInterpreter,
            IDomainNameResolver domainNameResolver)
        {
            this.fileTypeInterpreter = fileTypeInterpreter;
            this.domainNameResolver = domainNameResolver;
        }

        public async Task<IEnumerable<string>> ExtractLinksFromTextAsync(string text)
        {
            var words = whitespaceExpression.Split(text);

            var validationTasks = words.Select(IsValidLinkAsync);
            return ExtractValidLinksFromWords(words, await Task.WhenAll(validationTasks));
        }

        static List<string> ExtractValidLinksFromWords(string[] words, bool[] validationResults)
        {
            var extractedLinks = new List<string>();
            for (var i = 0; i < validationResults.Length; i++)
            {
                if (validationResults[i])
                {
                    extractedLinks.Add(words[i]);
                }
            }

            return extractedLinks;
        }

        public LinkType GetLinkType(string link)
        {
            var linkType = default(LinkType);

            if (fileTypeInterpreter.GetFileTypeFromFileName(link) == FileType.Image)
            {
                linkType |= LinkType.ImageFile;
            }

            if (link.StartsWith("https://"))
            {
                linkType |= LinkType.Https;
            }
            else if (link.StartsWith("http://"))
            {
                linkType |= LinkType.Http;
            }

            return linkType;
        }

        public async Task<bool> HasLinkAsync(string text)
        {
            var links = await ExtractLinksFromTextAsync(text);
            return links.Any();
        }

        public async Task<bool> HasLinkOfTypeAsync(string text, LinkType linkType)
        {
            var links = await ExtractLinksFromTextAsync(text);
            return links.Any(link => GetLinkType(link).HasFlag(linkType));
        }

        public async Task<bool> IsValidLinkAsync(string link)
        {
            var match = linkValidationExpression.Match(link);
            if(!match.Success)
            {
                return false;
            }

            var domain = match.Groups[1].Value;
            return linkValidationExpression.IsMatch(link) && await domainNameResolver.IsValidDomainAsync(domain);
        }
    }
}

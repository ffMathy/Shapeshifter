using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    class LinkParser : ILinkParser
    {
        private static readonly Regex linkValidationExpression;
        private static readonly Regex whitespaceExpression;

        static LinkParser()
        {
            linkValidationExpression = new Regex(@"^(?:(?:https?|ftp):\/\/)?(?:\S+(?::\S*)?@)?(?:(?!10(?:\.\d{1,3}){3})(?!127(?:\.\d{1,3}){3})(?!169\.254(?:\.\d{1,3}){2})(?!192\.168(?:\.\d{1,3}){2})(?!172\.(?:1[6-9]|2\d|3[0-1])(?:\.\d{1,3}){2})(?:[1-9]\d?|1\d\d|2[01]\d|22[0-3])(?:\.(?:1?\d{1,2}|2[0-4]\d|25[0-5])){2}(?:\.(?:[1-9]\d?|1\d\d|2[0-4]\d|25[0-4]))|(?:(?:[a-z\u00A1-\uFFFF0-9]+-?)*[a-z\u00A1-\uFFFF0-9]+)(?:\.(?:[a-z\u00A1-\uFFFF0-9]+-?)*[a-z\u00A1-\uFFFF0-9]+)*(?:\.(?:[a-z\u00A1-\uFFFF]{2,})))(?::\d{2,5})?(?:\/?[^\s]*)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            whitespaceExpression = new Regex(@"\s", RegexOptions.Compiled);
        }

        public IEnumerable<string> ExtractLinksFromText(string text)
        {
            var words = whitespaceExpression.Split(text);
            return words.Where(IsValidLink);
        }

        public LinkType GetLinkType(string link)
        {
            if (!IsValidLink(link))
            {
                throw new ArgumentException("The given link is not valid.", nameof(link));
            }

            var linkType = default(LinkType);

            if (GetIsImageType(link))
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

        private bool GetIsImageType(string link)
        {
            var imageTypes = new[] { ".jpg", ".png" };
            return imageTypes.Any(link.EndsWith);
        }

        public bool HasLink(string text)
        {
            return ExtractLinksFromText(text).Any();
        }

        public bool HasLinkOfType(string text, LinkType linkType)
        {
            throw new NotImplementedException();
        }

        public bool IsValidLink(string link)
        {
            return linkValidationExpression.IsMatch(link);
        }
    }
}

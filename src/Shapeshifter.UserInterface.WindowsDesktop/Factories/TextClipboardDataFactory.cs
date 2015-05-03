using System;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    class TextClipboardDataFactory : IClipboardDataFactory
    {
        public IClipboardData Build(string format, object data)
        {
            throw new NotImplementedException();
        }

        public bool CanBuild(string format)
        {
            return format == "CF_OEMTEXT" || format == "CF_TEXT" || format == "CF_UNICODETEXT";
        }
    }
}

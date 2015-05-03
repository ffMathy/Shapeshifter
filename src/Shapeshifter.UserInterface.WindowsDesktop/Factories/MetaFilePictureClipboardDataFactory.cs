using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    class MetaFilePictureClipboardDataFactory : IClipboardDataFactory
    {
        public IClipboardData Build(string format, object data)
        {
            throw new NotImplementedException();
        }

        public bool CanBuild(string format)
        {
            return format == "CF_METAFILEPICT" || format == "CF_ENHMETAFILE";
        }
    }
}

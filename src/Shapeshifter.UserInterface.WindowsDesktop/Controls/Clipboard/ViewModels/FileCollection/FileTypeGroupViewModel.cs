using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shapeshifter.Core.Data;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection
{
    class FileTypeGroupViewModel
    {
        public FileTypeGroupViewModel(IEnumerable<ClipboardFileData> data)
        {
            Count = data.Count();
            FileType = Path.GetExtension(data.First().FileName);
            Icon = data.First().FileIcon;
        }

        public int Count { get; private set; }

        public string FileType { get; private set; }

        public byte[] Icon { get; private set; }
    }
}

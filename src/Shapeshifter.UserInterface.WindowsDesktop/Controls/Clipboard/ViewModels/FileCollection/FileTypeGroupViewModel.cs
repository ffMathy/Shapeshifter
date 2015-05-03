using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shapeshifter.Core.Data;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection
{
    public class FileTypeGroupViewModel
    {
        public FileTypeGroupViewModel(IEnumerable<ClipboardFileData> data)
        {
            this.Count = data.Count();
            this.FileType = Path.GetExtension(data.First().FileName);
            this.Icon = data.First().FileIcon;
        }

        public int Count { get; private set; }

        public string FileType { get; private set; }

        public byte[] Icon { get; private set; }
    }
}

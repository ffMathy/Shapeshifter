#region

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection
{
    internal class FileTypeGroupViewModel : IFileTypeGroupViewModel
    {
        public FileTypeGroupViewModel(IEnumerable<IClipboardFileData> data)
        {
            Count = data.Count();
            FileType = Path.GetExtension(data.First().FileName);
            Icon = data.First().FileIcon;
        }

        public int Count { get; }

        public string FileType { get; }

        public byte[] Icon { get; }
    }
}
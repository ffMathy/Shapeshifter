using System;
using System.Collections.Generic;
using System.IO;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection
{
    internal class FileTypeGroupViewModel : IFileTypeGroupViewModel
    {
        public FileTypeGroupViewModel(
            IReadOnlyList<IClipboardFileData> data)
        {
            if (data.Count == 0)
            {
                throw new ArgumentException("There are no files in the collecton given.", nameof(data));
            }

            Count = data.Count;

            var firstData = data[0];
            FileType = Path.GetExtension(firstData.FileName);
            Icon = firstData.FileIcon;
        }

        public int Count { get; }

        public string FileType { get; }

        public byte[] Icon { get; }
    }
}
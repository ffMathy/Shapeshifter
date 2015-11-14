using System.Linq;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Files
{
    internal class FileTypeInterpreter : IFileTypeInterpreter
    {
        public FileType GetFileTypeFromFileName(string name)
        {
            if (IsImageFileType(name))
            {
                return FileType.Image;
            }
            if (IsTextFileType(name))
            {
                return FileType.Text;
            }

            return FileType.Other;
        }

        private static bool IsTextFileType(string name)
        {
            var imageFileTypes = new[] {".txt"};
            return imageFileTypes.Any(name.EndsWith);
        }

        private static bool IsImageFileType(string name)
        {
            var imageFileTypes = new[] {".png", ".jpg"};
            return imageFileTypes.Any(name.EndsWith);
        }
    }
}
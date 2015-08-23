using Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces;
using System.Linq;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Files
{
    class FileTypeInterpreter : IFileTypeInterpreter
    {
        public FileType GetFileTypeFromFileName(string name)
        {
            if (IsImageFileType(name))
            {
                return FileType.Image;
            } else if(IsTextFileType(name))
            {
                return FileType.Text;
            }

            return FileType.Other;
        }

        bool IsTextFileType(string name)
        {
            var imageFileTypes = new[] { ".txt" };
            return imageFileTypes.Any(name.EndsWith);
        }

        static bool IsImageFileType(string name)
        {
            var imageFileTypes = new[] { ".png", ".jpg" };
            return imageFileTypes.Any(name.EndsWith);
        }
    }
}

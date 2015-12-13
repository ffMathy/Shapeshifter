namespace Shapeshifter.WindowsDesktop.Services.Files
{
    using System.Linq;

    using Interfaces;

    class FileTypeInterpreter: IFileTypeInterpreter
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

        static bool IsTextFileType(string name)
        {
            var imageFileTypes = new[]
            {
                ".txt"
            };
            return imageFileTypes.Any(name.EndsWith);
        }

        static bool IsImageFileType(string name)
        {
            var imageFileTypes = new[]
            {
                ".png",
                ".jpg"
            };
            return imageFileTypes.Any(name.EndsWith);
        }
    }
}